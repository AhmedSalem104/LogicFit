using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Expenses.Commands.UpdateExpense;

public class UpdateExpenseCommandHandler : IRequestHandler<UpdateExpenseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateExpenseCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Expense", request.Id);

        expense.BranchId = request.BranchId;
        expense.CategoryId = request.CategoryId;
        expense.Amount = request.Amount;
        expense.ExpenseDate = request.ExpenseDate;
        expense.Description = request.Description;
        expense.VendorName = request.VendorName;
        expense.PaymentMethod = request.PaymentMethod;
        expense.ReceiptImageUrl = request.ReceiptImageUrl;
        expense.ReferenceNumber = request.ReferenceNumber;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
