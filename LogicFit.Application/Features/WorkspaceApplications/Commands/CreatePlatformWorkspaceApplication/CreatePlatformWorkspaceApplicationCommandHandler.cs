using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.CreatePlatformWorkspaceApplication;

public sealed class CreatePlatformWorkspaceApplicationCommandHandler
    : IRequestHandler<CreatePlatformWorkspaceApplicationCommand, PlatformWorkspaceApplicationCreatedDto>
{
    private static readonly ApplicationRequestStatus[] OpenStatuses =
    [
        ApplicationRequestStatus.Draft,
        ApplicationRequestStatus.Submitted,
        ApplicationRequestStatus.UnderReview,
        ApplicationRequestStatus.NeedsMoreInformation
    ];

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _clock;
    private readonly ICurrentUserService _currentUser;

    public CreatePlatformWorkspaceApplicationCommandHandler(
        IApplicationDbContext context,
        IDateTimeService clock,
        ICurrentUserService currentUser)
        => (_context, _clock, _currentUser) = (context, clock, currentUser);

    public async Task<PlatformWorkspaceApplicationCreatedDto> Handle(
        CreatePlatformWorkspaceApplicationCommand request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        var normalizedEmail = IdentityEmailAddress.Normalize(request.OwnerEmail);
        var normalizedPhone = PhoneNumberNormalizer.NormalizeOptional(request.OwnerPhoneNumber);
        var identifier = request.WorkspaceIdentifier.Trim().ToLowerInvariant();

        var plan = await _context.Plans
            .Include(x => x.PlanFeatures)
            .ThenInclude(x => x.Feature)
            .FirstOrDefaultAsync(x => x.Id == request.PlanId && x.IsActive && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        var identifierTaken = await _context.Tenants.IgnoreQueryFilters()
            .AnyAsync(x => x.Subdomain == identifier && !x.IsDeleted, cancellationToken)
            || await _context.ApplicationRequests.AnyAsync(x =>
                x.ReservedWorkspaceIdentifier == identifier && OpenStatuses.Contains(x.Status), cancellationToken);
        if (identifierTaken)
            throw new ConflictException("This workspace identifier is already reserved.");

        var identity = await _context.IdentityAccounts
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        var newIdentity = identity is null;
        string? temporaryPassword = null;

        if (identity is not null)
        {
            if (!identity.IsActive)
                throw new ConflictException("The owner Global Identity is inactive.");
        }
        else
        {
            if (normalizedPhone is not null && await _context.IdentityAccounts
                    .AnyAsync(x => x.NormalizedPhoneNumber == normalizedPhone, cancellationToken))
                throw new ConflictException("An identity already uses this phone number.");

            temporaryPassword = GenerateTemporaryPassword();
            identity = new IdentityAccount
            {
                FullName = request.OwnerFullName.Trim(),
                Email = request.OwnerEmail.Trim(),
                NormalizedEmail = normalizedEmail,
                PhoneNumber = request.OwnerPhoneNumber?.Trim(),
                NormalizedPhoneNumber = normalizedPhone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
                IsActive = true,
                EmailVerifiedAt = _clock.UtcNow
            };
            _context.IdentityAccounts.Add(identity);
        }

        var targetScopeKey = $"workspace:{identifier}";
        var duplicateApplication = await _context.ApplicationRequests.AnyAsync(x =>
            x.IdentityAccountId == identity.Id &&
            x.ApplicationType == ToApplicationType(request.WorkspaceType) &&
            x.TargetScopeKey == targetScopeKey &&
            OpenStatuses.Contains(x.Status), cancellationToken);
        if (duplicateApplication)
            throw new ConflictException("An active application already exists for this identity and workspace.");

        var now = _clock.UtcNow;
        var planSnapshot = PlanSnapshotFactory.Create(plan, request.BillingCycle, now);
        var workspace = new Tenant
        {
            Name = string.IsNullOrWhiteSpace(request.BrandName) ? request.WorkspaceName.Trim() : request.BrandName.Trim(),
            Subdomain = identifier,
            WorkspaceType = request.WorkspaceType,
            Status = TenantStatus.PendingApproval,
            Email = identity.Email,
            PhoneNumber = identity.PhoneNumber,
            Description = request.Description?.Trim(),
            Address = request.Address?.Trim(),
            BrandingSettings = new LogicFit.Domain.ValueObjects.BrandingSettings
            {
                AppName = string.IsNullOrWhiteSpace(request.BrandName) ? request.WorkspaceName.Trim() : request.BrandName.Trim(),
                PrimaryColor = request.WorkspaceType == WorkspaceType.FreelanceCoach ? "#7C3AED" : "#2563EB",
                SecondaryColor = request.WorkspaceType == WorkspaceType.FreelanceCoach ? "#0F766E" : "#1D4ED8"
            }
        };
        _context.Tenants.Add(workspace);

        var application = new ApplicationRequest
        {
            IdentityAccountId = identity.Id,
            ApplicationType = ToApplicationType(request.WorkspaceType),
            Status = ApplicationRequestStatus.Submitted,
            TargetScopeKey = targetScopeKey,
            ReservedWorkspaceIdentifier = identifier,
            RequestedRole = request.WorkspaceType == WorkspaceType.FreelanceCoach ? UserRole.FreelanceOwner : UserRole.Owner,
            PlanId = plan.Id,
            BillingCycle = request.BillingCycle,
            PlanSnapshotJson = planSnapshot,
            PlanSnapshotAtUtc = now,
            PayloadJson = JsonSerializer.Serialize(new FreelanceWorkspaceApplicationPayload
            {
                WorkspaceName = request.WorkspaceName.Trim(),
                WorkspaceIdentifier = identifier,
                OwnerFullName = request.OwnerFullName.Trim(),
                BrandName = TrimOrNull(request.BrandName),
                Bio = TrimOrNull(request.Description),
                Specialties = SplitValues(request.Specialization),
                WelcomeMessage = TrimOrNull(request.DeliveryMode),
                MustChangePassword = newIdentity
            }),
            SubmittedAt = now
        };
        application.ProvisionedWorkspaceId = workspace.Id;
        _context.ApplicationRequests.Add(application);
        _context.ApplicationRequestRevisions.Add(new ApplicationRequestRevision
        {
            ApplicationRequestId = application.Id,
            RevisionNumber = 1,
            PayloadJson = application.PayloadJson,
            SubmittedAt = now,
            SubmittedBy = _currentUser.UserId ?? "platform-admin"
        });

        var subscription = new TenantSubscription
        {
            TenantId = workspace.Id,
            PlanId = plan.Id,
            Status = TenantSubscriptionStatus.PendingPayment,
            BillingCycle = request.BillingCycle,
            Amount = plan.Price,
            Currency = plan.Currency
        };
        _context.TenantSubscriptions.Add(subscription);
        _context.PaymentRequests.Add(new PaymentRequest
        {
            TenantId = workspace.Id,
            TenantSubscriptionId = subscription.Id,
            ApplicationRequestId = application.Id,
            IdentityAccountId = identity.Id,
            PlanId = plan.Id,
            BillingCycle = request.BillingCycle,
            PlanSnapshotJson = planSnapshot,
            IdempotencyKey = $"platform-workspace:{application.Id:N}",
            Amount = plan.Price,
            Currency = plan.Currency,
            Status = PaymentRequestStatus.PendingReview,
            Operation = PaymentRequestOperation.NewSubscription
        });

        await _context.SaveChangesAsync(cancellationToken);

        var lifecycle = new PlatformApplicationLifecycleDto
        {
            WorkspaceType = request.WorkspaceType,
            PaymentStatus = PaymentRequestStatus.PendingReview,
            WorkspaceStatus = TenantStatus.PendingApproval,
            SubscriptionStatus = TenantSubscriptionStatus.PendingPayment,
            DatabaseStatus = DatabaseResourceStatus.Available,
            DatabaseStatusCode = "Unassigned",
            RequiredAction = "مراجعة الطلب",
            NextStep = "فحص بيانات المالك والدفع ثم بدء المراجعة",
            UserMessage = "تم إنشاء الطلب وينتظر مراجعة إدارة المنصة.",
            CanAccessDashboard = false,
            LastUpdatedAtUtc = now
        };

        return new PlatformWorkspaceApplicationCreatedDto
        {
            Application = PlatformApplicationMapper.ToDto(application, identity.Email, identity.PhoneNumber, lifecycle),
            NewIdentity = newIdentity,
            OneTimeCredentials = temporaryPassword is null
                ? null
                : new OneTimeOwnerCredentialsDto
                {
                    Email = identity.Email,
                    TemporaryPassword = temporaryPassword,
                    MustChangePassword = true
                }
        };
    }

    private static void Validate(CreatePlatformWorkspaceApplicationCommand request)
    {
        if (request.WorkspaceType is not (WorkspaceType.Gym or WorkspaceType.FreelanceCoach))
            throw new ValidationException("WorkspaceType", "WorkspaceType must be Gym or FreelanceCoach.");
        if (string.IsNullOrWhiteSpace(request.WorkspaceName))
            throw new ValidationException("WorkspaceName", "Workspace name is required.");
        if (string.IsNullOrWhiteSpace(request.WorkspaceIdentifier))
            throw new ValidationException("WorkspaceIdentifier", "Workspace identifier is required.");
        if (string.IsNullOrWhiteSpace(request.OwnerFullName))
            throw new ValidationException("OwnerFullName", "Owner full name is required.");
        if (string.IsNullOrWhiteSpace(request.OwnerEmail))
            throw new ValidationException("OwnerEmail", "Owner email is required.");
        if (request.PlanId == Guid.Empty)
            throw new ValidationException("PlanId", "A plan is required.");
    }

    private static ApplicationType ToApplicationType(WorkspaceType workspaceType) =>
        workspaceType == WorkspaceType.FreelanceCoach
            ? ApplicationType.FreelanceWorkspaceCreation
            : ApplicationType.GymWorkspaceCreation;

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyList<string> SplitValues(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789-_";
        var bytes = RandomNumberGenerator.GetBytes(18);
        var builder = new StringBuilder(bytes.Length);
        foreach (var value in bytes)
            builder.Append(alphabet[value % alphabet.Length]);
        return builder.ToString();
    }
}
