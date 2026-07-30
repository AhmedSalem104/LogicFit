using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.WorkspaceClientJoins.DTOs;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceClientJoins.Commands.PreviewWorkspaceClientJoin;

public sealed class PreviewWorkspaceClientJoinCommandHandler : IRequestHandler<PreviewWorkspaceClientJoinCommand, WorkspaceClientJoinPreviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public PreviewWorkspaceClientJoinCommandHandler(IApplicationDbContext context, IDateTimeService dateTimeService)
        => (_context, _dateTimeService) = (context, dateTimeService);

    public async Task<WorkspaceClientJoinPreviewDto> Handle(PreviewWorkspaceClientJoinCommand request, CancellationToken cancellationToken)
    {
        var joinCode = await _context.WorkspaceClientJoinCodes
            .Include(x => x.Tenant)
            .SingleOrDefaultAsync(x => x.CodeHash == IdentityEmailActionToken.Hash(request.Code), cancellationToken);
        if (joinCode is null || joinCode.RevokedAt.HasValue || joinCode.ExpiresAt <= _dateTimeService.UtcNow || joinCode.Tenant.IsDeleted)
            throw new ConflictException("This join code is invalid or expired.");
        return new WorkspaceClientJoinPreviewDto
        {
            WorkspaceId = joinCode.TenantId,
            WorkspaceName = joinCode.Tenant.Name,
            WorkspaceIdentifier = joinCode.Tenant.Subdomain,
            LogoUrl = joinCode.Tenant.LogoUrl,
            ExpiresAt = joinCode.ExpiresAt,
            RequiresWorkspaceApproval = !joinCode.AutoApproveClients
        };
    }
}
