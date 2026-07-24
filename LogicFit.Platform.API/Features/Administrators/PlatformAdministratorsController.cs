using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Platform.API.Features.Administrators;

[ApiController]
[Route("api/platform/administrators")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformAdministratorsController(IApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var users = await context.Users.AsNoTracking().IgnoreQueryFilters()
            .Where(u => !u.IsDeleted && (u.Role == UserRole.PlatformOwner || u.Role == UserRole.PlatformAdmin))
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email, u.PhoneNumber, Role = u.Role.ToString(), u.IsActive, u.CreatedAt, FullName = u.Profile == null ? null : u.Profile.FullName })
            .ToListAsync(cancellationToken);
        return Ok(users);
    }
}
