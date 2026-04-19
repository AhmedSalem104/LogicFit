using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using MediatR;

namespace LogicFit.Application.Features.ExpenseCategories.Commands.CreateExpenseCategory;

public class CreateExpenseCategoryCommandHandler : IRequestHandler<CreateExpenseCategoryCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateExpenseCategoryCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.GetCurrentTenantId(),
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId,
            IsActive = request.IsActive
        };

        _context.ExpenseCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}
