using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Platform.API.Features.Reports;

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
}
