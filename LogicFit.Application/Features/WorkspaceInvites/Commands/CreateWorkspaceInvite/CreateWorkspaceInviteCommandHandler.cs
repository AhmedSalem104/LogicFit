using System.Net;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.WorkspaceInvites.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EmailTokenGenerator = LogicFit.Application.Features.Identity.IdentityEmailActionToken;

namespace LogicFit.Application.Features.WorkspaceInvites.Commands.CreateWorkspaceInvite;

public sealed class CreateWorkspaceInviteCommandHandler : IRequestHandler<CreateWorkspaceInviteCommand, WorkspaceInviteCreatedDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IWorkspaceMembershipQuotaService _quotaService;
    private readonly IEmailSender _emailSender;
    private readonly IIdentityEmailLinkFactory _linkFactory;

    public CreateWorkspaceInviteCommandHandler(IApplicationDbContext context, ITenantService tenantService,
        ICurrentUserService currentUserService, IDateTimeService dateTimeService,
        IWorkspaceMembershipQuotaService quotaService, IEmailSender emailSender, IIdentityEmailLinkFactory linkFactory)
        => (_context, _tenantService, _currentUserService, _dateTimeService, _quotaService, _emailSender, _linkFactory)
            = (context, tenantService, currentUserService, dateTimeService, quotaService, emailSender, linkFactory);

    public async Task<WorkspaceInviteCreatedDto> Handle(CreateWorkspaceInviteCommand request, CancellationToken cancellationToken)
    {
        if (!_emailSender.IsConfigured || !_linkFactory.IsConfigured)
            throw new ServiceUnavailableException("WORKSPACE_INVITES_EMAIL_NOT_CONFIGURED", "Workspace invitations are temporarily unavailable.");
        var workspaceId = _tenantService.GetCurrentTenantId();
        var workspace = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == workspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), workspaceId);
        if (workspace.WorkspaceType != WorkspaceType.FreelanceCoach)
            throw new ConflictException("Team invitations are only available in a freelance workspace.");
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            throw new ForbiddenException("A workspace user is required.");
        var inviter = await _context.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.TenantId == workspaceId && x.UserId == userId &&
                x.Status == WorkspaceMembershipStatus.Active && !x.IsDeleted, cancellationToken);
        if (inviter?.Role != UserRole.FreelanceOwner)
            throw new ForbiddenException("Only the Freelance Owner can invite team members.");

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var isOwnerEmail = await _context.IdentityAccounts.AnyAsync(x => x.Id == inviter.IdentityAccountId && x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (isOwnerEmail)
            throw new ConflictException("The workspace owner already has a membership.");
        var existingMember = await _context.WorkspaceMemberships.IgnoreQueryFilters()
            .Include(x => x.IdentityAccount)
            .AnyAsync(x => x.TenantId == workspaceId && x.IdentityAccount.NormalizedEmail == normalizedEmail && !x.IsDeleted, cancellationToken);
        if (existingMember)
            throw new ConflictException("This email already has a membership in the workspace.");
        var now = _dateTimeService.UtcNow;
        var activeInvite = await _context.WorkspaceInvites.AnyAsync(x => x.TenantId == workspaceId &&
            x.NormalizedEmail == normalizedEmail && x.Role == request.RequestedRole &&
            x.Status == WorkspaceInviteStatus.Pending && x.ExpiresAt > now, cancellationToken);
        if (activeInvite)
            throw new ConflictException("An active invitation already exists for this email and role.");
        await _quotaService.EnsureCapacityAsync(workspaceId, request.RequestedRole, cancellationToken);

        var rawToken = EmailTokenGenerator.CreateRaw();
        var invite = new WorkspaceInvite
        {
            TenantId = workspaceId,
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            Role = request.RequestedRole,
            InvitedByMembershipId = inviter.Id,
            TokenHash = EmailTokenGenerator.Hash(rawToken),
            ExpiresAt = now.AddDays(7)
        };
        _context.WorkspaceInvites.Add(invite);
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Type = "workspace.invite.created",
            Payload = $"{{\"inviteId\":\"{invite.Id}\",\"workspaceId\":\"{workspaceId}\"}}",
            OccurredAtUtc = now,
            IdempotencyKey = $"workspace-invite:{invite.Id}:created"
        });
        await _context.SaveChangesAsync(cancellationToken);

        var link = _linkFactory.CreateWorkspaceInvitationLink(rawToken);
        try
        {
            var safeWorkspaceName = WebUtility.HtmlEncode(workspace.Name);
            await _emailSender.SendAsync(new EmailMessage(invite.Email, $"Invitation to {workspace.Name}",
                $"<p>You were invited to join <strong>{safeWorkspaceName}</strong> as {invite.Role}.</p><p><a href=\"{WebUtility.HtmlEncode(link)}\">Review invitation</a></p><p>This invitation expires in 7 days.</p>",
                $"You were invited to join {workspace.Name} as {invite.Role}. Review the invitation: {link}\n\nThis invitation expires in 7 days."), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            invite.Status = WorkspaceInviteStatus.Revoked;
            invite.RevokedAt = _dateTimeService.UtcNow;
            await _context.SaveChangesAsync(CancellationToken.None);
            throw new ServiceUnavailableException("WORKSPACE_INVITES_EMAIL_UNAVAILABLE", "Workspace invitation delivery is temporarily unavailable. Please try again later.");
        }
        return new WorkspaceInviteCreatedDto { InviteId = invite.Id, EmailMasked = WorkspaceInviteSupport.MaskEmail(invite.Email), Role = invite.Role, ExpiresAt = invite.ExpiresAt };
    }
}
