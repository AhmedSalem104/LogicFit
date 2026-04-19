using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Payments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Payments.Queries.GetPayments;

public class GetPaymentsQueryHandler : IRequestHandler<GetPaymentsQuery, List<PaymentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetPaymentsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<PaymentDto>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Payments
            .Include(p => p.Invoice)
            .Include(p => p.Branch)
            .Include(p => p.Client)
            .Include(p => p.ReceivedBy)
            .Where(p => p.TenantId == tenantId)
            .AsQueryable();

        if (request.ClientId.HasValue)
            query = query.Where(p => p.ClientId == request.ClientId.Value);
        if (request.BranchId.HasValue)
            query = query.Where(p => p.BranchId == request.BranchId.Value);
        if (request.InvoiceId.HasValue)
            query = query.Where(p => p.InvoiceId == request.InvoiceId.Value);
        if (request.SubscriptionId.HasValue)
            query = query.Where(p => p.SubscriptionId == request.SubscriptionId.Value);
        if (request.Method.HasValue)
            query = query.Where(p => p.Method == request.Method.Value);
        if (request.FromDate.HasValue)
            query = query.Where(p => p.ReceivedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(p => p.ReceivedAt <= request.ToDate.Value);

        var payments = await query.OrderByDescending(p => p.ReceivedAt).ToListAsync(cancellationToken);

        return payments.Select(p => new PaymentDto
        {
            Id = p.Id,
            TenantId = p.TenantId,
            InvoiceId = p.InvoiceId,
            InvoiceNumber = p.Invoice?.InvoiceNumber,
            SubscriptionId = p.SubscriptionId,
            BranchId = p.BranchId,
            BranchName = p.Branch?.Name,
            ClientId = p.ClientId,
            ClientName = p.Client?.Email,
            Amount = p.Amount,
            Method = p.Method,
            ReceivedAt = p.ReceivedAt,
            ReceivedByName = p.ReceivedBy?.Email,
            ReceiptNumber = p.ReceiptNumber,
            Notes = p.Notes,
            ReferenceNumber = p.ReferenceNumber
        }).ToList();
    }
}
