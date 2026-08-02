using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.API.Features.Platform.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Platform.Backups;

[ApiController]
[Route("api/platform/backups")]
[Authorize(Policy = Permissions.ManagePlatformBackups)]
public sealed class PlatformBackupsController(IBackupService backupService) : ControllerBase
{
    [HttpGet]
    public ActionResult<PlatformPage<BackupRecord>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PlatformPaging.DefaultPageSize) =>
        Ok(PlatformPaging.Create(backupService.List(), page, pageSize));

    [HttpGet("status")]
    public ActionResult<BackupStatus> Status() => Ok(backupService.GetStatus());

    [HttpGet("{fileName}/download")]
    public IActionResult Download(string fileName)
    {
        try
        {
            var backup = backupService.OpenRead(fileName);
            return File(backup.Content, "application/octet-stream", backup.FileName, enableRangeProcessing: false);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "خدمة النسخ الاحتياطي غير متاحة",
                detail: ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<BackupRecord>> Create(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await backupService.CreateAsync(cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "خدمة النسخ الاحتياطي غير مهيأة",
                detail: ex.Message);
        }
    }

    [HttpPost("batch")]
    public async Task<ActionResult<BackupBatchDto>> CreateBatch(
        [FromBody] BackupBatchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await backupService.CreateBatchAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Backup service is not ready.", detail: ex.Message);
        }
    }

    [HttpGet("batches")]
    public ActionResult<IReadOnlyList<BackupBatchDto>> Batches([FromQuery] int take = 50)
        => Ok(backupService.ListBatches(take));

    [HttpPost("batches/{batchId:guid}/retry")]
    public async Task<ActionResult<BackupBatchDto>> Retry(Guid batchId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await backupService.RetryBatchAsync(batchId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { errorCode = "BACKUP_RETRY_NOT_ALLOWED", message = ex.Message });
        }
    }
}
