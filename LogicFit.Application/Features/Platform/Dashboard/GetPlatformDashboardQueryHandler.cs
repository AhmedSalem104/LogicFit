using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Dashboard;

public class GetPlatformDashboardQueryHandler : IRequestHandler<GetPlatformDashboardQuery, PlatformDashboardDto>
{
    private readonly IApplicationDbContext _context;

    public GetPlatformDashboardQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformDashboardDto> Handle(GetPlatformDashboardQuery request, CancellationToken cancellationToken)
    {
        var tenants = _context.Tenants.Where(t => t.Id != PlatformConstants.PlatformTenantId);
        var subscriptions = _context.TenantSubscriptions.AsNoTracking().IgnoreQueryFilters();
        if (request.TenantId.HasValue) subscriptions = subscriptions.Where(x => x.TenantId == request.TenantId.Value);
        if (request.PlanId.HasValue) subscriptions = subscriptions.Where(x => x.PlanId == request.PlanId.Value);
        if (request.SubscriptionStatus.HasValue) subscriptions = subscriptions.Where(x => x.Status == request.SubscriptionStatus.Value);
        if (request.FromUtc.HasValue) subscriptions = subscriptions.Where(x => x.CreatedAt >= request.FromUtc.Value);
        if (request.ToUtc.HasValue) subscriptions = subscriptions.Where(x => x.CreatedAt < request.ToUtc.Value);
        var invoices = _context.SubscriptionInvoices.AsNoTracking().IgnoreQueryFilters();
        var payments = _context.SubscriptionPayments.AsNoTracking();

        return new PlatformDashboardDto
        {
            TotalGyms = await tenants.CountAsync(cancellationToken),
            ActiveGyms = await tenants.CountAsync(t => t.Status == TenantStatus.Active, cancellationToken),
            TrialGyms = await tenants.CountAsync(t => t.Status == TenantStatus.Trial, cancellationToken),
            PendingApprovalGyms = await tenants.CountAsync(t => t.Status == TenantStatus.PendingApproval, cancellationToken),
            SuspendedGyms = await tenants.CountAsync(t => t.Status == TenantStatus.Suspended, cancellationToken),
            TotalMembers = await _context.Users.IgnoreQueryFilters().CountAsync(u => u.Role == UserRole.Client && !u.IsDeleted, cancellationToken),
            ExpiredSubscriptions = await subscriptions.CountAsync(x => x.Status == TenantSubscriptionStatus.Expired && !x.IsDeleted, cancellationToken),
            ActiveSubscriptions = await subscriptions.CountAsync(x => x.Status == TenantSubscriptionStatus.Active && !x.IsDeleted, cancellationToken),
            PendingPayments = await _context.PaymentRequests.IgnoreQueryFilters().CountAsync(x => x.Status == PaymentRequestStatus.Pending && !x.IsDeleted, cancellationToken),
            InvoiceCount = await invoices.CountAsync(x => !x.IsDeleted, cancellationToken),
            InvoicedAmount = await invoices.Where(x => !x.IsDeleted).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m,
            CollectedAmount = await payments.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m,
            FeatureCount = await _context.Features.CountAsync(cancellationToken),
            QuotaDefinitionCount = await _context.FeatureQuotaDefinitions.CountAsync(x => x.IsActive, cancellationToken),
            FailedJobs = await _context.JobExecutionLogs.CountAsync(x => x.Status == "Failed", cancellationToken),
            FailedOutbox = await _context.OutboxMessages.CountAsync(x => x.FailedAtUtc != null && x.ProcessedAtUtc == null, cancellationToken)
        };
    }
}
