using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.ExpenseCategories.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ExpenseCategories.Queries.GetExpenseCategories;

public class GetExpenseCategoriesQueryHandler : IRequestHandler<GetExpenseCategoriesQuery, List<ExpenseCategoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetExpenseCategoriesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ExpenseCategoryDto>> Handle(GetExpenseCategoriesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.ExpenseCategories
            .Include(c => c.ParentCategory)
            .Include(c => c.Children)
            .Where(c => c.TenantId == tenantId)
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        var categories = await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);

        return categories.Select(c => new ExpenseCategoryDto
        {
            Id = c.Id,
            TenantId = c.TenantId,
            Name = c.Name,
            Description = c.Description,
            ParentCategoryId = c.ParentCategoryId,
            ParentCategoryName = c.ParentCategory?.Name,
            IsActive = c.IsActive,
            ChildrenCount = c.Children.Count(ch => !ch.IsDeleted)
        }).ToList();
    }
}
