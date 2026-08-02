using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LogicFit.API.Features.Platform.Restores;

[ApiController]
[Route("api/platform/restores")]
[Authorize(Policy = Permissions.ManagePlatformBackups)]
public sealed class PlatformRestoresController(
    IDatabaseRestoreService restoreService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("capabilities")]
    public ActionResult<DatabaseRestoreCapabilities> Capabilities() => Ok(restoreService.GetCapabilities());

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RestoreJobDto>>> List(CancellationToken cancellationToken)
        => Ok(await restoreService.ListAsync(cancellationToken));

    [HttpPost("reauthenticate")]
    [EnableRateLimiting("sensitive-action")]
    public async Task<ActionResult<SensitiveActionGrantDto>> Reauthenticate(
        [FromBody] PlatformPasswordReauthenticationRequest request,
        CancellationToken cancellationToken)
    {
        EnsurePlatformOwner();
        var userId = GetUserId();
        return Ok(await restoreService.ReauthenticateAsync(userId, request.CurrentPassword, cancellationToken));
    }

    [HttpPost]
    [EnableRateLimiting("sensitive-action")]
    public async Task<ActionResult<RestoreJobDto>> Restore(
        [FromBody] PlatformRestoreRequest request,
        CancellationToken cancellationToken)
    {
        EnsurePlatformOwner();
        var job = await restoreService.RestoreAsync(GetUserId(), request.GrantToken,
            new DatabaseRestoreRequest(request.TenantId, request.SourceDatabaseBackupId,
                request.TargetDatabaseResourceId, request.WorkspaceNameConfirmation, request.Reason), cancellationToken);
        return Ok(job);
    }

    private void EnsurePlatformOwner()
    {
        if (!User.IsInRole(SystemRoles.PlatformOwner))
            throw new ForbiddenException("Only PlatformOwner can perform database restore.");
    }

    private Guid GetUserId()
        => Guid.TryParse(currentUser.UserId, out var id) && id != Guid.Empty
            ? id
            : throw new UnauthorizedException("An authenticated platform user is required.");
}

public sealed record PlatformPasswordReauthenticationRequest(string CurrentPassword);
public sealed record PlatformRestoreRequest(
    Guid TenantId,
    Guid SourceDatabaseBackupId,
    Guid? TargetDatabaseResourceId,
    string WorkspaceNameConfirmation,
    string Reason,
    string GrantToken);
