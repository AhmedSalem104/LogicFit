using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.Dashboard;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using LogicFit.API.Features.Platform.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.Dashboard;

[ApiController]
[Route("api/platform/dashboard")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformDashboardController(IMediator mediator, IApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PlatformDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformDashboardDto>> Get([FromQuery] GetPlatformDashboardQuery query)
        => Ok(await mediator.Send(query));

    [HttpGet("tenants")]
    public async Task<IActionResult> Tenants([FromQuery] string? search = null, [FromQuery] TenantStatus? status = null, [FromQuery] Guid? planId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = PlatformPaging.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var tenants = context.Tenants.AsNoTracking().IgnoreQueryFilters().Where(x => x.Id != PlatformConstants.PlatformTenantId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search)) tenants = tenants.Where(x => x.Name.Contains(search) || (x.Subdomain != null && x.Subdomain.Contains(search)));
        if (status.HasValue) tenants = tenants.Where(x => x.Status == status.Value);
        var subscriptions = context.TenantSubscriptions.AsNoTracking().IgnoreQueryFilters().Where(x => !x.IsDeleted);
        if (planId.HasValue) subscriptions = subscriptions.Where(x => x.PlanId == planId.Value);

        // Keep the member count out of the correlated SQL projection. On some production
        // EF/SQL combinations that shape is not translatable and makes this dashboard-only
        // list return 500. The platform read is explicitly cross-tenant, so both queries
        // bypass tenant filters and the small page is merged in memory.
        var paged = await PlatformPaging.CreateAsync(tenants.OrderBy(x => x.Name).Select(tenant => new DashboardTenantSummaryRow
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            Status = tenant.Status,
            CreatedAt = tenant.CreatedAt,
            Subscription = subscriptions.Where(subscription => subscription.TenantId == tenant.Id).OrderByDescending(subscription => subscription.CreatedAt).Select(subscription => new DashboardSubscriptionSummaryRow
            {
                Status = subscription.Status,
                EndDate = subscription.EndDate,
                PlanId = subscription.PlanId,
                PlanName = subscription.Plan.Name
            }).FirstOrDefault()
        }), page, pageSize, cancellationToken);

        var tenantIds = paged.Items.Select(x => x.Id).ToArray();
        if (tenantIds.Length > 0)
        {
            var memberCounts = await context.Users.AsNoTracking().IgnoreQueryFilters()
                .Where(user => tenantIds.Contains(user.TenantId) && user.Role == UserRole.Client && !user.IsDeleted)
                .GroupBy(user => user.TenantId)
                .Select(group => new { TenantId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

            foreach (var tenant in paged.Items)
                tenant.MembersCount = memberCounts.GetValueOrDefault(tenant.Id);
        }

        return Ok(paged);
    }

    private sealed class DashboardTenantSummaryRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Subdomain { get; init; }
        public TenantStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public int MembersCount { get; set; }
        public DashboardSubscriptionSummaryRow? Subscription { get; init; }
    }

    private sealed class DashboardSubscriptionSummaryRow
    {
        public TenantSubscriptionStatus Status { get; init; }
        public DateTime? EndDate { get; init; }
        public Guid PlanId { get; init; }
        public string PlanName { get; init; } = string.Empty;
    }
}
