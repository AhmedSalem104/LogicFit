using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
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
        var application = new ApplicationRequest
        {
            IdentityAccountId = identity.Id,
            ApplicationType = ApplicationType.FreelanceWorkspaceCreation,
            Status = ApplicationRequestStatus.Submitted,
            TargetScopeKey = WorkspaceCreationScope,
            ReservedWorkspaceIdentifier = identifier,
            RequestedRole = UserRole.FreelanceOwner,
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
        _context.ApplicationRequests.Add(application);
        _context.ApplicationRequestRevisions.Add(new ApplicationRequestRevision
        {
            ApplicationRequestId = application.Id,
            RevisionNumber = 1,
            PayloadJson = application.PayloadJson,
            SubmittedAt = now,
            SubmittedBy = identity.Id.ToString()
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

    private static string? NormalizePhone(string? value) => string.IsNullOrWhiteSpace(value)
        ? null
        : new string(value.Where(char.IsDigit).ToArray());

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
