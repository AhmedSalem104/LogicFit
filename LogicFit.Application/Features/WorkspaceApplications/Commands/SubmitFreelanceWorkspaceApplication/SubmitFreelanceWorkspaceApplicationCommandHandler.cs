using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.SubmitFreelanceWorkspaceApplication;

public sealed class SubmitFreelanceWorkspaceApplicationCommandHandler
    : IRequestHandler<SubmitFreelanceWorkspaceApplicationCommand, ApplicationTrackingSessionDto>
{
    private const string WorkspaceCreationScope = "freelance-workspace";
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;

    public SubmitFreelanceWorkspaceApplicationCommandHandler(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _currentUserService = currentUserService;
    }

    public async Task<ApplicationTrackingSessionDto> Handle(
        SubmitFreelanceWorkspaceApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var normalizedPhone = NormalizePhone(request.PhoneNumber);
        var identifier = request.WorkspaceIdentifier.Trim().ToLowerInvariant();

        var identity = await _context.IdentityAccounts
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (identity is null)
        {
            if (normalizedPhone is not null && await _context.IdentityAccounts
                    .AnyAsync(x => x.NormalizedPhoneNumber == normalizedPhone, cancellationToken))
            {
                throw new ConflictException("An identity already uses this phone number.");
            }

            identity = new IdentityAccount
            {
                Email = request.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                PhoneNumber = request.PhoneNumber?.Trim(),
                NormalizedPhoneNumber = normalizedPhone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true
            };
            _context.IdentityAccounts.Add(identity);
        }
        else if (!identity.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, identity.PasswordHash))
        {
            throw new UnauthorizedException("Invalid credentials");
        }

        var existingPayment = await _context.PaymentRequests
            .Include(x => x.ApplicationRequest)
            .FirstOrDefaultAsync(x => x.IdentityAccountId == identity.Id &&
                                      x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (existingPayment?.ApplicationRequest is not null)
            return await CreateTrackingSessionAsync(existingPayment.ApplicationRequest, _dateTimeService.UtcNow, cancellationToken);

        var plan = await _context.Plans
            .Include(x => x.PlanFeatures)
            .ThenInclude(x => x.Feature)
            .FirstOrDefaultAsync(x => x.Id == request.PlanId && x.IsActive && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);
        if (request.PaymentAmount != plan.Price)
            throw new ValidationException("PaymentAmount", "The submitted amount must match the selected plan snapshot.");

        var duplicateExists = await _context.ApplicationRequests.AnyAsync(x =>
            x.IdentityAccountId == identity.Id &&
            x.ApplicationType == ApplicationType.FreelanceWorkspaceCreation &&
            x.TargetScopeKey == WorkspaceCreationScope &&
            (x.Status == ApplicationRequestStatus.Draft ||
             x.Status == ApplicationRequestStatus.Submitted ||
             x.Status == ApplicationRequestStatus.UnderReview ||
             x.Status == ApplicationRequestStatus.NeedsMoreInformation), cancellationToken);
        if (duplicateExists)
            throw new ConflictException("An active freelance workspace application already exists for this identity.");

        var identifierTaken = await _context.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Subdomain == identifier, cancellationToken)
            || await _context.ApplicationRequests.AnyAsync(x =>
                x.ReservedWorkspaceIdentifier == identifier &&
                (x.Status == ApplicationRequestStatus.Draft ||
                 x.Status == ApplicationRequestStatus.Submitted ||
                 x.Status == ApplicationRequestStatus.UnderReview ||
                 x.Status == ApplicationRequestStatus.NeedsMoreInformation), cancellationToken);
        if (identifierTaken)
            throw new ConflictException("This workspace identifier is already reserved.");

        var now = _dateTimeService.UtcNow;
        var planSnapshot = PlanSnapshotFactory.Create(plan, request.BillingCycle, now);
        var workspace = new Tenant
        {
            Name = string.IsNullOrWhiteSpace(request.BrandName) ? request.WorkspaceName.Trim() : request.BrandName.Trim(),
            Subdomain = identifier,
            WorkspaceType = WorkspaceType.FreelanceCoach,
            Status = TenantStatus.PendingApproval,
            Email = identity.Email,
            PhoneNumber = identity.PhoneNumber
        };
        _context.Tenants.Add(workspace);
        var application = new ApplicationRequest
        {
            IdentityAccountId = identity.Id,
            ApplicationType = ApplicationType.FreelanceWorkspaceCreation,
            Status = ApplicationRequestStatus.Submitted,
            TargetScopeKey = WorkspaceCreationScope,
            ReservedWorkspaceIdentifier = identifier,
            RequestedRole = UserRole.FreelanceOwner,
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
                LogoUrl = TrimOrNull(request.LogoUrl),
                PhotoUrl = TrimOrNull(request.PhotoUrl),
                CoverImageUrl = TrimOrNull(request.CoverImageUrl),
                BackgroundImageUrl = TrimOrNull(request.BackgroundImageUrl),
                PrimaryColor = TrimOrNull(request.PrimaryColor),
                SecondaryColor = TrimOrNull(request.SecondaryColor),
                Bio = TrimOrNull(request.Bio),
                Specialties = request.Specialties?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray() ?? Array.Empty<string>(),
                Certifications = request.Certifications?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray() ?? Array.Empty<string>(),
                SocialLinks = request.SocialLinks ?? new Dictionary<string, string>(),
                WelcomeMessage = TrimOrNull(request.WelcomeMessage),
                BookingSettings = request.BookingSettings
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
            SubmittedBy = identity.Id.ToString()
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
        var paymentRequest = new PaymentRequest
        {
            TenantId = workspace.Id,
            TenantSubscriptionId = subscription.Id,
            ApplicationRequestId = application.Id,
            IdentityAccountId = identity.Id,
            PlanId = plan.Id,
            BillingCycle = request.BillingCycle,
            PlanSnapshotJson = planSnapshot,
            IdempotencyKey = request.IdempotencyKey.Trim(),
            Amount = plan.Price,
            Currency = plan.Currency,
            TransactionNumber = request.PaymentTransactionNumber?.Trim(),
            PaymentDate = request.PaymentDate ?? now,
            Status = PaymentRequestStatus.PendingReview,
            Operation = PaymentRequestOperation.NewSubscription
        };
        _context.PaymentRequests.Add(paymentRequest);
        _context.PaymentProofs.Add(new PaymentProof
        {
            PaymentRequestId = paymentRequest.Id,
            Version = 1,
            StorageKey = request.ProofStorageKey.Trim(),
            OriginalFileName = request.ProofOriginalFileName.Trim(),
            ContentType = request.ProofContentType,
            SizeBytes = request.ProofSizeBytes,
            Sha256 = request.ProofSha256.ToUpperInvariant(),
            UploadedAtUtc = now,
            UploadedBy = identity.Id.ToString()
        });

        var rawToken = ApplicationTrackingToken.CreateRaw();
        var trackingSession = new ApplicationTrackingSession
        {
            ApplicationRequestId = application.Id,
            TokenHash = ApplicationTrackingToken.Hash(rawToken),
            ExpiresAt = now.AddMinutes(30),
            CreatedByIp = _currentUserService.IpAddress
        };
        _context.ApplicationTrackingSessions.Add(trackingSession);
        await _context.SaveChangesAsync(cancellationToken);

        return new ApplicationTrackingSessionDto(application.Id, application.Status, rawToken, trackingSession.ExpiresAt);
    }

    private async Task<ApplicationTrackingSessionDto> CreateTrackingSessionAsync(
        ApplicationRequest application,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var rawToken = ApplicationTrackingToken.CreateRaw();
        var session = new ApplicationTrackingSession
        {
            ApplicationRequestId = application.Id,
            TokenHash = ApplicationTrackingToken.Hash(rawToken),
            ExpiresAt = now.AddMinutes(30),
            CreatedByIp = _currentUserService.IpAddress
        };
        _context.ApplicationTrackingSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return new ApplicationTrackingSessionDto(application.Id, application.Status, rawToken, session.ExpiresAt);
    }

    private static string? NormalizePhone(string? value) => string.IsNullOrWhiteSpace(value)
        ? null
        : new string(value.Where(char.IsDigit).ToArray());

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
