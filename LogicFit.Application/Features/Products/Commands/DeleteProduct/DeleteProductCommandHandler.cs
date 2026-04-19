using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteProductCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Product", request.Id);

        var hasSales = await _context.SaleItems.AnyAsync(s => s.ProductId == product.Id, cancellationToken);
        if (hasSales)
            throw new DomainException("Cannot delete a product that has sales history. Deactivate instead.");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
