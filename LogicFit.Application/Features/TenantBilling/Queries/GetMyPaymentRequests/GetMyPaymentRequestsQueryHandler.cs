using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.TenantBilling.Queries.GetMyPaymentRequests;

public class GetMyPaymentRequestsQueryHandler : IRequestHandler<GetMyPaymentRequestsQuery, List<PaymentRequestDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetMyPaymentRequestsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<PaymentRequestDto>> Handle(GetMyPaymentRequestsQuery request, CancellationToken cancellationToken)
    {
        // PaymentRequests are not tenant query-filtered (platform-owned), so filter by TenantId explicitly.
        var tenantId = _tenantService.GetCurrentTenantId();

        return await _context.PaymentRequests
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentRequestDto
            {
                Id = p.Id,
                TenantId = p.TenantId,
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
