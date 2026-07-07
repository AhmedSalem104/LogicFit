using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.TenantBilling.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.TenantBilling.Queries.GetMyInvoices;

public class GetMyInvoicesQueryHandler : IRequestHandler<GetMyInvoicesQuery, List<SubscriptionInvoiceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetMyInvoicesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<SubscriptionInvoiceDto>> Handle(GetMyInvoicesQuery request, CancellationToken cancellationToken)
    {
        // SubscriptionInvoices are platform-owned (no tenant filter) — filter by TenantId explicitly.
        var tenantId = _tenantService.GetCurrentTenantId();

        return await _context.SubscriptionInvoices
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.IssueDate)
            .Select(i => new SubscriptionInvoiceDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                Amount = i.Amount,
                Currency = i.Currency,
                Status = i.Status,
                IssueDate = i.IssueDate,
                DueDate = i.DueDate,
                PaidAt = i.PaidAt
            })
            .ToListAsync(cancellationToken);
    }
}
