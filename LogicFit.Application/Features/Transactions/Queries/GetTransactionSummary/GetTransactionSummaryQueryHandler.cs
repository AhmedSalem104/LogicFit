using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Transactions.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Transactions.Queries.GetTransactionSummary;

public class GetTransactionSummaryQueryHandler : IRequestHandler<GetTransactionSummaryQuery, TransactionSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetTransactionSummaryQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<TransactionSummaryDto> Handle(GetTransactionSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.WalletTransactions
            .Where(t => t.TenantId == tenantId)
            .AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(t => t.UserId == request.UserId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(t => t.CreatedAt <= request.ToDate.Value);

        var transactions = await query.ToListAsync(cancellationToken);

        return new TransactionSummaryDto
        {
            TotalDeposits = transactions.Where(t => t.Type == TransactionType.Deposit).Sum(t => t.Amount),
            TotalWithdrawals = transactions.Where(t => t.Type == TransactionType.Withdrawal).Sum(t => t.Amount),
            TotalPayments = transactions.Where(t => t.Type == TransactionType.Payment).Sum(t => t.Amount),
            TotalRefunds = transactions.Where(t => t.Type == TransactionType.Refund).Sum(t => t.Amount),
            NetBalance = transactions.Sum(t =>
                t.Type == TransactionType.Deposit || t.Type == TransactionType.Refund
                    ? t.Amount
                    : -t.Amount),
            TransactionCount = transactions.Count
        };
    }
}
