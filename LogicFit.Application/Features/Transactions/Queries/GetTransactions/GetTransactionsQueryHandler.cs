using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Transactions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Transactions.Queries.GetTransactions;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, List<TransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetTransactionsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.WalletTransactions
            .Where(t => t.TenantId == tenantId)
            .AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(t => t.UserId == request.UserId.Value);

        if (request.Type.HasValue)
            query = query.Where(t => t.Type == request.Type.Value);

        if (request.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(t => t.CreatedAt <= request.ToDate.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                TenantId = t.TenantId,
                UserId = t.UserId,
                UserName = t.User.Profile != null ? t.User.Profile.FullName : t.User.Email,
                Type = t.Type,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Description = t.Description,
                ReferenceType = t.ReferenceType,
                ReferenceId = t.ReferenceId,
                CreatedAt = t.CreatedAt,
                CreatedBy = t.CreatedBy
            })
            .ToListAsync(cancellationToken);
    }
}
