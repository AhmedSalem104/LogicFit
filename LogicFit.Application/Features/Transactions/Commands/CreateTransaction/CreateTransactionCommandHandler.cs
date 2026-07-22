using LogicFit.Application.Common.Interfaces;
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

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.TenantId == tenantId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // Get user's current balance from last transaction
        var lastTransaction = await _context.WalletTransactions
            .Where(t => t.TenantId == tenantId && t.UserId == request.UserId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var currentBalance = lastTransaction?.BalanceAfter ?? 0;

        // Calculate new balance based on transaction type
        var balanceChange = request.Type switch
        {
            TransactionType.Deposit => request.Amount,
            TransactionType.Refund => request.Amount,
            TransactionType.Withdrawal => -request.Amount,
            TransactionType.Payment => -request.Amount,
            TransactionType.Adjustment => request.Amount, // Can be positive or negative
            _ => 0
        };

        var newBalance = currentBalance + balanceChange;

        if ((request.Type == TransactionType.Withdrawal || request.Type == TransactionType.Payment)
            && request.Amount > user.WalletBalance)
            throw new ValidationException("Amount", "Insufficient wallet balance");

        if (newBalance < 0)
            throw new ValidationException("Amount", "Wallet balance cannot become negative");

        user.WalletBalance = newBalance;

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

        return transaction.Id;
    }
}
