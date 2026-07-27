using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using LogicFit.API.Features.Platform.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.Alerts;

[ApiController]
[Route("api/platform/alerts")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformAlertsController(IApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = PlatformPaging.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var alerts = new List<object>();
        var failedJobs = await context.JobExecutionLogs.AsNoTracking().Where(x => x.Status == "Failed").OrderByDescending(x => x.StartedAtUtc).Take(20).ToListAsync(cancellationToken);
        alerts.AddRange(failedJobs.Select(x => new { severity = "error", title = "فشل مهمة دورية", message = x.JobName, occurredAtUtc = x.StartedAtUtc, detail = x.Error }));
        var failedOutbox = await context.OutboxMessages.AsNoTracking().Where(x => x.FailedAtUtc != null && x.ProcessedAtUtc == null).OrderByDescending(x => x.FailedAtUtc).Take(20).ToListAsync(cancellationToken);
        alerts.AddRange(failedOutbox.Select(x => new { severity = "error", title = "فشل رسالة Outbox", message = x.Type, occurredAtUtc = x.FailedAtUtc!.Value, detail = x.LastError }));
        var pending = await context.PaymentRequests.AsNoTracking().IgnoreQueryFilters().CountAsync(x => x.Status == PaymentRequestStatus.Pending && !x.IsDeleted, cancellationToken);
        if (pending > 0) alerts.Add(new { severity = "warning", title = "طلبات دفع معلقة", message = $"يوجد {pending} طلب بانتظار المراجعة", occurredAtUtc = DateTime.UtcNow, detail = (string?)null });
        var ordered = alerts.OrderByDescending(x => x.GetType().GetProperty("occurredAtUtc")?.GetValue(x)).ToList();
        return Ok(PlatformPaging.Create(ordered, page, pageSize));
    }
}
