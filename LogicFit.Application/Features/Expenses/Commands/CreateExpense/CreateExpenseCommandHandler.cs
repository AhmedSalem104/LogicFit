using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Expenses.Commands.CreateExpense;

public class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public CreateExpenseCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Guid> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var categoryExists = await _context.ExpenseCategories
            .AnyAsync(c => c.Id == request.CategoryId && c.TenantId == tenantId, cancellationToken);
        if (!categoryExists)
            throw new NotFoundException("ExpenseCategory", request.CategoryId);

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = request.BranchId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate ?? _dateTimeService.UtcNow,
            Description = request.Description,
            VendorName = request.VendorName,
            PaymentMethod = request.PaymentMethod,
            ReceiptImageUrl = request.ReceiptImageUrl,
            ReferenceNumber = request.ReferenceNumber
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);
        return expense.Id;
    }
}
