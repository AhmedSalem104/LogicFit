using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using LogicFit.API.Features.Platform.Common;
using LogicFit.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.Administrators;

[ApiController]
[Route("api/platform/administrators")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformAdministratorsController(IApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = PlatformPaging.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var users = context.Users.AsNoTracking().IgnoreQueryFilters()
            .Where(u => !u.IsDeleted && (u.Role == UserRole.PlatformOwner || u.Role == UserRole.PlatformAdmin))
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email, u.PhoneNumber, Role = u.Role.ToString(), u.IsActive, u.CreatedAt, FullName = u.Profile == null ? null : u.Profile.FullName });
        return Ok(await PlatformPaging.CreateAsync(users, page, pageSize, cancellationToken));
    }

    public sealed record CreateAdministratorRequest(string Email, string Password, string FullName);

    [HttpPost]
    [Authorize(Policy = OtpStepUpRequirement.PolicyName)]
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

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = OtpStepUpRequirement.PolicyName)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] bool isActive, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(SystemRoles.PlatformOwner)) return Forbid();
        var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        if (user is null || user.Role is not (UserRole.PlatformAdmin or UserRole.PlatformOwner)) return NotFound();
        if (user.Role == UserRole.PlatformOwner) return BadRequest(new { message = "لا يمكن تعطيل مالك المنصة." });
        user.IsActive = isActive;
        user.PermissionsVersion++;
        await context.SaveChangesAsync(cancellationToken);
        return Ok(new { user.Id, user.IsActive });
    }
}
