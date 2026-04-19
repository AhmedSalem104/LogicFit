using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateProductCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        if (!string.IsNullOrEmpty(request.Sku))
        {
            var dup = await _context.Products.AnyAsync(p => p.TenantId == tenantId && p.Sku == request.Sku, cancellationToken);
            if (dup) throw new ConflictException("SKU already exists");
        }

        if (!string.IsNullOrEmpty(request.Barcode))
        {
            var dup = await _context.Products.AnyAsync(p => p.TenantId == tenantId && p.Barcode == request.Barcode, cancellationToken);
            if (dup) throw new ConflictException("Barcode already exists");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Barcode = request.Barcode,
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            TaxRate = request.TaxRate,
            Unit = request.Unit,
            ImageUrl = request.ImageUrl,
            IsActive = request.IsActive,
            MinStockLevel = request.MinStockLevel,
            TrackStock = request.TrackStock
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}
