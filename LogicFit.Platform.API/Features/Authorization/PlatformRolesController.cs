using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Platform.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Platform.API.Features.Authorization;

[ApiController]
[Route("api/platform/roles")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformRolesController(IApplicationDbContext context) : ControllerBase
{
    public sealed record UpdateRolePermissionsRequest(IReadOnlyList<string> PermissionCodes);
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = PlatformPaging.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var roles = context.AppRoles.AsNoTracking()
            .Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, Permissions = x.RolePermissions.Select(p => p.Permission.Code).OrderBy(c => c).ToList() });
        return Ok(await PlatformPaging.CreateAsync(roles, page, pageSize, cancellationToken));
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissionCatalog(CancellationToken cancellationToken)
        => Ok(await context.Permissions.AsNoTracking().OrderBy(x => x.Category).ThenBy(x => x.Code)
            .Select(x => new { x.Code, x.DisplayName, x.Category, x.IsPlatformPermission }).ToListAsync(cancellationToken));

    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(SystemRoles.PlatformOwner)) return Forbid();
        var role = await context.AppRoles.Include(x => x.RolePermissions).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (role is null) return NotFound();
        if (role.IsSystemRole && role.Name == SystemRoles.PlatformOwner) return BadRequest(new { message = "لا يمكن تعديل صلاحيات مالك المنصة." });
        var codes = request.PermissionCodes.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var permissions = await context.Permissions.Where(x => codes.Contains(x.Code)).ToListAsync(cancellationToken);
        if (permissions.Count != codes.Length) return BadRequest(new { message = "توجد صلاحيات غير معروفة." });
        foreach (var assignment in role.RolePermissions.ToList()) context.RolePermissions.Remove(assignment);
        foreach (var permission in permissions) context.RolePermissions.Add(new LogicFit.Domain.Entities.RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        var users = await context.UserRoleAssignments.Where(x => x.RoleId == role.Id).Select(x => x.User).ToListAsync(cancellationToken);
        foreach (var user in users) user.PermissionsVersion++;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
