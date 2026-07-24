using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Platform.API.Features.Authorization;

[ApiController]
[Route("api/platform/roles")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformRolesController(IApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var roles = await context.AppRoles.AsNoTracking()
            .Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, Permissions = x.RolePermissions.Select(p => p.Permission.Code).OrderBy(c => c).ToList() })
            .ToListAsync(cancellationToken);
        return Ok(roles);
    }
}
