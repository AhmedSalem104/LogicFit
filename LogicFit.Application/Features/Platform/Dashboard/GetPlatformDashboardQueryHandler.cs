using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Dashboard;

public class GetPlatformDashboardQueryHandler : IRequestHandler<GetPlatformDashboardQuery, PlatformDashboardDto>
{
    private readonly IApplicationDbContext _context;

    public GetPlatformDashboardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformDashboardDto> Handle(GetPlatformDashboardQuery request, CancellationToken cancellationToken)
    {
        var tenants = _context.Tenants.Where(t => t.Id != PlatformConstants.PlatformTenantId);

        return new PlatformDashboardDto
        {
            TotalGyms = await tenants.CountAsync(cancellationToken),
            ActiveGyms = await tenants.CountAsync(t => t.Status == TenantStatus.Active, cancellationToken),
            TrialGyms = await tenants.CountAsync(t => t.Status == TenantStatus.Trial, cancellationToken),
            PendingApprovalGyms = await tenants.CountAsync(t => t.Status == TenantStatus.PendingApproval, cancellationToken),
            SuspendedGyms = await tenants.CountAsync(t => t.Status == TenantStatus.Suspended, cancellationToken),
            TotalMembers = await _context.Users.IgnoreQueryFilters()
                .CountAsync(u => u.Role == UserRole.Client && !u.IsDeleted, cancellationToken)
        };
    }
}
