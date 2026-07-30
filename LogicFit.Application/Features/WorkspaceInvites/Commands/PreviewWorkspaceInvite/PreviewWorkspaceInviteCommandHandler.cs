using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.Identity.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceInvites.Commands.PreviewWorkspaceInvite;

public sealed class PreviewWorkspaceInviteCommandHandler : IRequestHandler<PreviewWorkspaceInviteCommand, WorkspaceInvitePreviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public PreviewWorkspaceInviteCommandHandler(IApplicationDbContext context, IDateTimeService dateTimeService)
        => (_context, _dateTimeService) = (context, dateTimeService);

    public async Task<WorkspaceInvitePreviewDto> Handle(PreviewWorkspaceInviteCommand request, CancellationToken cancellationToken)
    {
        var invite = await _context.WorkspaceInvites
            .Include(x => x.Tenant)
            .SingleOrDefaultAsync(x => x.TokenHash == IdentityEmailActionToken.Hash(request.Token), cancellationToken);
        if (invite is null || invite.Status != WorkspaceInviteStatus.Pending || invite.ExpiresAt <= _dateTimeService.UtcNow)
            throw new ConflictException("This invitation is invalid, expired, or has already been used.");
        return new WorkspaceInvitePreviewDto
        {
            InviteId = invite.Id,
            WorkspaceId = invite.TenantId,
            WorkspaceName = invite.Tenant.Name,
            WorkspaceIdentifier = invite.Tenant.Subdomain,
            LogoUrl = invite.Tenant.LogoUrl,
            Role = invite.Role,
            EmailMasked = WorkspaceInviteSupport.MaskEmail(invite.Email),
            ExpiresAt = invite.ExpiresAt
        };
    }
}
