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
        var tenants = context.Tenants.AsNoTracking().Where(x => x.Id != PlatformConstants.PlatformTenantId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search)) tenants = tenants.Where(x => x.Name.Contains(search) || (x.Subdomain != null && x.Subdomain.Contains(search)));
        if (status.HasValue) tenants = tenants.Where(x => x.Status == status.Value);
        var subscriptions = context.TenantSubscriptions.AsNoTracking().IgnoreQueryFilters().Where(x => !x.IsDeleted);
        if (planId.HasValue) subscriptions = subscriptions.Where(x => x.PlanId == planId.Value);
        return Ok(await PlatformPaging.CreateAsync(tenants.OrderBy(x => x.Name).Select(tenant => new
        {
            tenant.Id, tenant.Name, tenant.Subdomain, tenant.Status, tenant.CreatedAt,
            MembersCount = context.Users.IgnoreQueryFilters().Count(user => user.TenantId == tenant.Id && user.Role == UserRole.Client && !user.IsDeleted),
            Subscription = subscriptions.Where(subscription => subscription.TenantId == tenant.Id).OrderByDescending(subscription => subscription.CreatedAt).Select(subscription => new { subscription.Status, subscription.EndDate, subscription.PlanId, PlanName = subscription.Plan.Name }).FirstOrDefault()
        }), page, pageSize, cancellationToken));
    }
}
