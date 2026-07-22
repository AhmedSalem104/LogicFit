using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.Platform.API.Features.Backups;

[ApiController]
[Route("api/platform/backups")]
[Authorize(Policy = Permissions.ManagePlatformBackups)]
public sealed class PlatformBackupsController(IBackupService backupService) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<BackupRecord>> List() => Ok(backupService.List());

    [HttpPost]
    public async Task<ActionResult<BackupRecord>> Create(CancellationToken cancellationToken)
        => Ok(await backupService.CreateAsync(cancellationToken));
}
