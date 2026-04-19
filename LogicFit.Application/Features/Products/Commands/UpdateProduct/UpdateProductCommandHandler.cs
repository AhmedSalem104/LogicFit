using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateProductCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Product", request.Id);

        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.Description = request.Description;
        product.Sku = request.Sku;
        product.Barcode = request.Barcode;
        product.CostPrice = request.CostPrice;
        product.SellingPrice = request.SellingPrice;
        product.TaxRate = request.TaxRate;
        product.Unit = request.Unit;
        product.ImageUrl = request.ImageUrl;
        product.IsActive = request.IsActive;
        product.MinStockLevel = request.MinStockLevel;
        product.TrackStock = request.TrackStock;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
