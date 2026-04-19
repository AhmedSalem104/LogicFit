using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ProductCategories.Commands.DeleteProductCategory;

public class DeleteProductCategoryCommandHandler : IRequestHandler<DeleteProductCategoryCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteProductCategoryCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(DeleteProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var category = await _context.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("ProductCategory", request.Id);

        var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == request.Id, cancellationToken);
        if (hasProducts)
            throw new DomainException("Cannot delete a category that has products");

        var hasChildren = await _context.ProductCategories.AnyAsync(c => c.ParentCategoryId == request.Id, cancellationToken);
        if (hasChildren)
            throw new DomainException("Cannot delete a category that has subcategories");

        _context.ProductCategories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
