using LogicFit.Application.Common.Authorization;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.API.Features.TenantBackups;

[ApiController]
[Route("api/tenant/backups")]
[Authorize(Policy = Permissions.CreateAndDownloadTenantBackup)]
[AllowWhenWorkspaceReadOnly]
public sealed class TenantBackupsController(
    ITenantBackupExportService exportService,
    ITenantService tenantService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("reauthenticate")]
    [EnableRateLimiting("sensitive-action")]
    public async Task<ActionResult<SensitiveActionGrantDto>> Reauthenticate(
        [FromBody] PasswordReauthenticationRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, tenantId) = GetScope();
        return Ok(await exportService.ReauthenticateAsync(userId, tenantId, request.CurrentPassword, cancellationToken));
    }

    [HttpPost("reauthenticate-download")]
    [EnableRateLimiting("sensitive-action")]
    public async Task<ActionResult<SensitiveActionGrantDto>> ReauthenticateForDownload(
        [FromBody] PasswordReauthenticationRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, tenantId) = GetScope();
        return Ok(await exportService.ReauthenticateForDownloadAsync(userId, tenantId, request.CurrentPassword, cancellationToken));
    }

    [HttpGet("exports")]
    public async Task<ActionResult<IReadOnlyList<TenantBackupExportDto>>> List(CancellationToken cancellationToken)
    {
        var (userId, tenantId) = GetScope();
        return Ok(await exportService.ListAsync(userId, tenantId, cancellationToken));
    }

    [HttpPost("exports")]
    [EnableRateLimiting("sensitive-action")]
    public async Task<ActionResult<TenantBackupExportDto>> Create(
        [FromBody] TenantBackupExportRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, tenantId) = GetScope();
        return Ok(await exportService.CreateAsync(userId, tenantId, request, cancellationToken));
    }

    [HttpGet("exports/{exportId:guid}")]
    public async Task<ActionResult<TenantBackupExportDto>> Get(Guid exportId, CancellationToken cancellationToken)
    {
        var (userId, tenantId) = GetScope();
        return Ok(await exportService.GetAsync(userId, tenantId, exportId, cancellationToken));
    }

    [HttpPost("exports/{exportId:guid}/download-grant")]
    [EnableRateLimiting("sensitive-action")]
    public async Task<ActionResult<TenantBackupDownloadGrantDto>> CreateDownloadGrant(
        Guid exportId, [FromBody] SensitiveGrantRequest request, CancellationToken cancellationToken)
    {
        var (userId, tenantId) = GetScope();
        return Ok(await exportService.CreateDownloadGrantAsync(userId, tenantId, exportId, request.GrantToken, cancellationToken));
    }

    [HttpGet("exports/{exportId:guid}/download")]
    public async Task<IActionResult> Download(
        Guid exportId,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var (userId, tenantId) = GetScope();
        var download = await exportService.OpenDownloadAsync(userId, tenantId, exportId, token, cancellationToken);
        return File(download.Content, "application/octet-stream", download.FileName, enableRangeProcessing: false);
    }

    private (Guid UserId, Guid TenantId) GetScope()
    {
        if (!Guid.TryParse(currentUser.UserId, out var userId) || userId == Guid.Empty)
            throw new UnauthorizedException("An authenticated user is required.");
        var tenantId = tenantService.GetCurrentTenantId();
        if (tenantId == Guid.Empty)
            throw new UnauthorizedException("An active workspace is required.");
        return (userId, tenantId);
    }
}

public sealed record PasswordReauthenticationRequest(string CurrentPassword);
public sealed record SensitiveGrantRequest(string GrantToken);
