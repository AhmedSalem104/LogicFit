using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Transactions.Commands.CreateTransaction;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateTransactionCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var balanceChange = request.Type switch
        {
            TransactionType.Deposit => request.Amount,
            TransactionType.Refund => request.Amount,
            TransactionType.Withdrawal => -request.Amount,
            TransactionType.Payment => -request.Amount,
            TransactionType.Adjustment => request.Amount, // Can be positive or negative
            _ => throw new ValidationException("Type", "Unsupported wallet transaction type")
        };

        await using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);
        var newBalance = await WalletBalanceOperations.ApplyAsync(
            _context,
            tenantId,
            request.UserId,
            balanceChange,
            cancellationToken);

        var transaction = new WalletTransaction
        {
            TenantId = tenantId,
            UserId = request.UserId,
            Type = request.Type,
            Amount = request.Amount,
            BalanceAfter = newBalance,
            Description = request.Description,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId
        };

        _context.WalletTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        return transaction.Id;
    }
}
