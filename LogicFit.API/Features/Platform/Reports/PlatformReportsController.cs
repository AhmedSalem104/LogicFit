using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.Platform.Reports;

[ApiController]
[Route("api/platform/reports")]
[Authorize(Policy = Permissions.ManagePlatformReports)]
public sealed class PlatformReportsController(IApplicationDbContext context) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var invoices = context.Invoices.AsNoTracking().IgnoreQueryFilters();
        var subscriptions = context.TenantSubscriptions.AsNoTracking().IgnoreQueryFilters();
        var paymentRequests = context.PaymentRequests.AsNoTracking().IgnoreQueryFilters();
        return Ok(new
        {
            invoiceCount = await invoices.CountAsync(cancellationToken),
            invoicedAmount = await invoices.SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m,
            collectedAmount = await invoices.SumAsync(x => (decimal?)x.AmountPaid, cancellationToken) ?? 0m,
            pendingPaymentRequests = await paymentRequests.CountAsync(x => x.Status == PaymentRequestStatus.Pending && !x.IsDeleted, cancellationToken),
            activeSubscriptions = await subscriptions.CountAsync(x => x.Status == TenantSubscriptionStatus.Active && !x.IsDeleted, cancellationToken),
            expiredSubscriptions = await subscriptions.CountAsync(x => x.Status == TenantSubscriptionStatus.Expired && !x.IsDeleted, cancellationToken)
        });
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken)
    {
        return Ok(new
        {
            gyms = await context.Tenants.AsNoTracking().CountAsync(x => !x.IsDeleted, cancellationToken),
            members = await context.Users.IgnoreQueryFilters().CountAsync(x => x.Role == UserRole.Client && !x.IsDeleted, cancellationToken),
            plans = await context.Plans.AsNoTracking().CountAsync(x => !x.IsDeleted, cancellationToken),
            features = await context.Features.AsNoTracking().CountAsync(cancellationToken),
            subscriptions = await context.TenantSubscriptions.IgnoreQueryFilters().CountAsync(x => !x.IsDeleted, cancellationToken),
            invoices = await context.SubscriptionInvoices.IgnoreQueryFilters().CountAsync(x => !x.IsDeleted, cancellationToken),
            payments = await context.SubscriptionPayments.AsNoTracking().CountAsync(cancellationToken),
            auditEntries = await context.AuditLogs.IgnoreQueryFilters().CountAsync(cancellationToken),
            jobRuns = await context.JobExecutionLogs.AsNoTracking().CountAsync(cancellationToken),
            outboxMessages = await context.OutboxMessages.AsNoTracking().CountAsync(cancellationToken)
        });
    }
}
