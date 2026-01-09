using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Transactions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Transactions.Queries.GetTransactionById;

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, TransactionDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetTransactionByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<TransactionDto?> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        return await _context.WalletTransactions
            .Where(t => t.Id == request.Id && t.TenantId == tenantId)
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
