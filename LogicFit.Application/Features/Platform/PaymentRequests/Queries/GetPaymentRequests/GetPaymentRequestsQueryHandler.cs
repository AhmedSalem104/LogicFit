using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.PaymentRequests.Queries.GetPaymentRequests;

public class GetPaymentRequestsQueryHandler : IRequestHandler<GetPaymentRequestsQuery, List<PaymentRequestDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPaymentRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PaymentRequestDto>> Handle(GetPaymentRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PaymentRequests.AsQueryable();
        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentRequestDto
            {
                Id = p.Id,
                TenantId = p.TenantId,
                TenantName = p.Tenant.Name,
                PlanId = p.PlanId,
                PlanName = p.Plan.Name,
                TenantSubscriptionId = p.TenantSubscriptionId,
                Amount = p.Amount,
                Currency = p.Currency,
                PaymentMethodId = p.PaymentMethodId,
                TransactionNumber = p.TransactionNumber,
                PaymentDate = p.PaymentDate,
                ProofFileUrl = p.ProofFileUrl,
                Notes = p.Notes,
                Status = p.Status,
                ReviewedBy = p.ReviewedBy,
                ReviewedAt = p.ReviewedAt,
                RejectReason = p.RejectReason,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
