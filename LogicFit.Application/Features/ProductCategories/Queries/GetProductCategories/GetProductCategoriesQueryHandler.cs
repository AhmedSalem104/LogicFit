using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ProductCategories.Queries.GetProductCategories;

public class GetProductCategoriesQueryHandler : IRequestHandler<GetProductCategoriesQuery, List<ProductCategoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetProductCategoriesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ProductCategoryDto>> Handle(GetProductCategoriesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.ProductCategories
            .Include(c => c.ParentCategory)
            .Include(c => c.Products)
            .Where(c => c.TenantId == tenantId).AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        var cats = await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);

        return cats.Select(c => new ProductCategoryDto
        {
            Id = c.Id,
            TenantId = c.TenantId,
            Name = c.Name,
            Description = c.Description,
            ParentCategoryId = c.ParentCategoryId,
            ParentCategoryName = c.ParentCategory?.Name,
            ImageUrl = c.ImageUrl,
            IsActive = c.IsActive,
            ProductsCount = c.Products.Count(p => !p.IsDeleted)
        }).ToList();
    }
}
