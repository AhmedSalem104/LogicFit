using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ExpenseCategories.Commands.DeleteExpenseCategory;

public class DeleteExpenseCategoryCommandHandler : IRequestHandler<DeleteExpenseCategoryCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteExpenseCategoryCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(DeleteExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var category = await _context.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("ExpenseCategory", request.Id);

        var hasChildren = await _context.ExpenseCategories.AnyAsync(c => c.ParentCategoryId == request.Id, cancellationToken);
        if (hasChildren)
            throw new DomainException("Cannot delete a category that has child categories");

        var hasExpenses = await _context.Expenses.AnyAsync(e => e.CategoryId == request.Id, cancellationToken);
        if (hasExpenses)
            throw new DomainException("Cannot delete a category that has expenses linked to it");

        _context.ExpenseCategories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
