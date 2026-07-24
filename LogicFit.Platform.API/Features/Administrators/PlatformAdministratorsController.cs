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

    public sealed record CreateAdministratorRequest(string Email, string Password, string FullName);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdministratorRequest request, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(SystemRoles.PlatformOwner)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Email) || request.Password.Length < 12 || string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { message = "البريد والاسم مطلوبان وكلمة المرور يجب ألا تقل عن 12 حرفًا." });
        var email = request.Email.Trim().ToLowerInvariant();
        if (await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email && !u.IsDeleted, cancellationToken))
            return Conflict(new { message = "البريد الإلكتروني مستخدم بالفعل." });
        var user = new LogicFit.Domain.Entities.User
        {
            TenantId = PlatformConstants.PlatformTenantId, Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.PlatformAdmin, IsActive = true
        };
        context.Users.Add(user);
        context.UserProfiles.Add(new LogicFit.Domain.Entities.UserProfile { UserId = user.Id, FullName = request.FullName.Trim() });
        await context.SaveChangesAsync(cancellationToken);
        return Created($"api/platform/administrators/{user.Id}", new { user.Id, user.Email, Role = user.Role.ToString(), FullName = request.FullName.Trim() });
    }
}
