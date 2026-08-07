using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceMembers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceMembers.Queries.GetWorkspaceMembers;

public sealed class GetWorkspaceMembersQueryHandler : IRequestHandler<GetWorkspaceMembersQuery, IReadOnlyList<WorkspaceMemberDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _clock;

    public GetWorkspaceMembersQueryHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService clock)
        => (_context, _tenantService, _clock) = (context, tenantService, clock);

    public async Task<IReadOnlyList<WorkspaceMemberDto>> Handle(GetWorkspaceMembersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.CurrentTenantId ?? throw new Domain.Exceptions.ForbiddenException("A workspace context is required.");
        var query = _context.WorkspaceMemberships
            .IgnoreQueryFilters()
            .Include(x => x.User).ThenInclude(x => x.Profile)
            .Include(x => x.IdentityAccount)
            .Where(x => x.TenantId == tenantId && WorkspaceMemberMapping.IsAllowedRole(x.Role));
        if (request.Role.HasValue)
            query = query.Where(x => x.Role == request.Role.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim();
            query = query.Where(x => x.IdentityAccount.Email.Contains(search) ||
                (x.IdentityAccount.FullName != null && x.IdentityAccount.FullName.Contains(search)) ||
                (x.User.PhoneNumber != null && x.User.PhoneNumber.Contains(search)));
        }

        var members = await query.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).ToListAsync(cancellationToken);
        var mapped = members.Select(x => WorkspaceMemberMapping.ToDto(x, _clock.UtcNow)).ToList();
        if (!string.IsNullOrWhiteSpace(request.AccessStatus))
            mapped = mapped.Where(x => x.AccessStatus.Equals(request.AccessStatus.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        return mapped;
    }
}
