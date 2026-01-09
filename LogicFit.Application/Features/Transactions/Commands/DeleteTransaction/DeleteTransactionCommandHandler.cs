using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Transactions.Commands.DeleteTransaction;

public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteTransactionCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var transaction = await _context.WalletTransactions
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.TenantId == tenantId, cancellationToken);

        if (transaction == null)
            return false;

        _context.WalletTransactions.Remove(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
