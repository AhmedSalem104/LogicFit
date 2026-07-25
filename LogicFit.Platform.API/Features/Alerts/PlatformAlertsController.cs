using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Platform.API.Features.Alerts;

[ApiController]
[Route("api/platform/alerts")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformAlertsController(IApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var alerts = new List<object>();
        var failedJobs = await context.JobExecutionLogs.AsNoTracking().Where(x => x.Status == "Failed").OrderByDescending(x => x.StartedAtUtc).Take(20).ToListAsync(cancellationToken);
        alerts.AddRange(failedJobs.Select(x => new { severity = "error", title = "فشل مهمة دورية", message = x.JobName, occurredAtUtc = x.StartedAtUtc, detail = x.Error }));
        var failedOutbox = await context.OutboxMessages.AsNoTracking().Where(x => x.FailedAtUtc != null && x.ProcessedAtUtc == null).OrderByDescending(x => x.FailedAtUtc).Take(20).ToListAsync(cancellationToken);
        alerts.AddRange(failedOutbox.Select(x => new { severity = "error", title = "فشل رسالة Outbox", message = x.Type, occurredAtUtc = x.FailedAtUtc!.Value, detail = x.LastError }));
        var pending = await context.PaymentRequests.AsNoTracking().IgnoreQueryFilters().CountAsync(x => x.Status == PaymentRequestStatus.Pending && !x.IsDeleted, cancellationToken);
        if (pending > 0) alerts.Add(new { severity = "warning", title = "طلبات دفع معلقة", message = $"يوجد {pending} طلب بانتظار المراجعة", occurredAtUtc = DateTime.UtcNow, detail = (string?)null });
        return Ok(alerts.OrderByDescending(x => x.GetType().GetProperty("occurredAtUtc")?.GetValue(x)).ToList());
    }
}
