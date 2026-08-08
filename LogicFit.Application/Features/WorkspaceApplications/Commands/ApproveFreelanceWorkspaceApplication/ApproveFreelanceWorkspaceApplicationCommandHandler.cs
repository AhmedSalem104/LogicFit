using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Services;
using LogicFit.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.ApproveFreelanceWorkspaceApplication;

/// <summary>
/// Idempotent, two-stage provisioning. The first save reserves exactly one Tenant in Provisioning;
/// a retry reuses it, and a second successful save makes it Active together with its owner/membership.
/// </summary>
public sealed class ApproveFreelanceWorkspaceApplicationCommandHandler
    : IRequestHandler<ApproveFreelanceWorkspaceApplicationCommand, PlatformApplicationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IWorkspaceProvisioningSaga _provisioningSaga;

    public ApproveFreelanceWorkspaceApplicationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        IWorkspaceProvisioningSaga provisioningSaga)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _provisioningSaga = provisioningSaga;
    }

    public async Task<PlatformApplicationDto> Handle(
        ApproveFreelanceWorkspaceApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _context.ApplicationRequests
            .Include(x => x.IdentityAccount)
            .FirstOrDefaultAsync(x => x.Id == request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApplicationRequest), request.ApplicationId);

        if (application.ApplicationType is not (ApplicationType.FreelanceWorkspaceCreation or ApplicationType.GymWorkspaceCreation))
            throw new ConflictException("This endpoint only provisions Gym or FreelanceCoach workspaces.");

        if (application.Status == ApplicationRequestStatus.Approved)
        {
            if (application.ProvisionedWorkspaceId.HasValue)
                await _provisioningSaga.RunAsync(application.Id, cancellationToken);
            return PlatformApplicationMapper.ToDto(application, application.IdentityAccount.Email, application.IdentityAccount.PhoneNumber);
        }

        if (!ApplicationRequestStateMachine.CanTransition(application.Status, ApplicationRequestStatus.Approved))
            throw new ConflictException("This application cannot be approved.");

        var ownerRole = application.ApplicationType == ApplicationType.FreelanceWorkspaceCreation
            ? SystemRoles.FreelanceOwner
            : SystemRoles.Owner;
        if (!await _context.AppRoles.IgnoreQueryFilters().AnyAsync(
                x => x.TenantId == null && x.Name == ownerRole && !x.IsDeleted,
                cancellationToken))
        {
            throw new ConflictException("Freelance roles are not seeded yet.");
        }

        var payload = DeserializePayload(application.PayloadJson);
        var payment = await _context.PaymentRequests
            .FirstOrDefaultAsync(x => x.ApplicationRequestId == application.Id, cancellationToken);
        if (payment?.Status != PaymentRequestStatus.Approved)
            throw new ConflictException("Payment approval is required before workspace provisioning.");
        _context.Entry(application).Property(nameof(ApplicationRequest.RowVersion)).OriginalValue = Convert.FromBase64String(request.RowVersion);
        var workspace = await GetOrCreateProvisioningWorkspace(application, payload, cancellationToken);
        workspace.Status = TenantStatus.PendingSubscription;
        application.Status = ApplicationRequestStatus.Approved;
        application.DecisionReason = null;
        application.InformationRequest = null;
        application.RequestedFieldsJson = null;
        application.ReviewedAt = _dateTimeService.UtcNow;
        application.ReviewedBy = _currentUserService.UserId;
        await _context.SaveChangesAsync(cancellationToken);
        await _provisioningSaga.RunAsync(application.Id, cancellationToken);

        return PlatformApplicationMapper.ToDto(application, application.IdentityAccount.Email, application.IdentityAccount.PhoneNumber);
    }

    private async Task<Tenant> GetOrCreateProvisioningWorkspace(
        ApplicationRequest application,
        FreelanceWorkspaceApplicationPayload payload,
        CancellationToken cancellationToken)
    {
        if (application.ProvisionedWorkspaceId.HasValue)
        {
            return await _context.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == application.ProvisionedWorkspaceId.Value, cancellationToken)
                ?? throw new ConflictException("The provisioned workspace record is missing.");
        }

        var identifierTaken = await _context.Tenants.IgnoreQueryFilters()
            .AnyAsync(x => x.Subdomain == payload.WorkspaceIdentifier, cancellationToken);
        if (identifierTaken)
            throw new ConflictException("The reserved workspace identifier is no longer available.");

        var workspace = new Tenant
        {
            Name = string.IsNullOrWhiteSpace(payload.BrandName) ? payload.WorkspaceName : payload.BrandName,
            Subdomain = payload.WorkspaceIdentifier,
            WorkspaceType = application.ApplicationType == ApplicationType.FreelanceWorkspaceCreation
                ? WorkspaceType.FreelanceCoach
                : WorkspaceType.Gym,
            Status = TenantStatus.Provisioning,
            Email = application.IdentityAccount.Email,
            PhoneNumber = application.IdentityAccount.PhoneNumber,
            Description = payload.Bio,
            LogoUrl = payload.LogoUrl,
            CoverImageUrl = payload.CoverImageUrl,
            BrandingSettings = new BrandingSettings
            {
                AppName = string.IsNullOrWhiteSpace(payload.BrandName) ? payload.WorkspaceName : payload.BrandName,
                LogoUrl = payload.LogoUrl,
                LogoIconUrl = payload.PhotoUrl,
                InvoiceLogoUrl = payload.LogoUrl,
                PrimaryColor = payload.PrimaryColor,
                SecondaryColor = payload.SecondaryColor,
                LoginBackgroundUrl = payload.BackgroundImageUrl,
                DashboardBannerUrl = payload.CoverImageUrl
            }
        };
        _context.Tenants.Add(workspace);
        application.ProvisionedWorkspaceId = workspace.Id;
        await _context.SaveChangesAsync(cancellationToken);
        return workspace;
    }

    private static FreelanceWorkspaceApplicationPayload DeserializePayload(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<FreelanceWorkspaceApplicationPayload>(payloadJson);
            if (payload is null || string.IsNullOrWhiteSpace(payload.WorkspaceName) || string.IsNullOrWhiteSpace(payload.WorkspaceIdentifier))
                throw new JsonException();
            return payload;
        }
        catch (JsonException)
        {
            throw new ValidationException("Payload", "The freelance application payload is invalid.");
        }
    }
}
