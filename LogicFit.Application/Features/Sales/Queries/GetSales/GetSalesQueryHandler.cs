using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Sales.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Sales.Queries.GetSales;

public class GetSalesQueryHandler : IRequestHandler<GetSalesQuery, List<SaleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetSalesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<SaleDto>> Handle(GetSalesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Sales
            .Include(s => s.Branch)
            .Include(s => s.Client)
            .Include(s => s.Cashier)
            .Include(s => s.Invoice)
            .Include(s => s.Items)
            .Where(s => s.TenantId == tenantId)
            .AsQueryable();

        if (request.BranchId.HasValue)
            query = query.Where(s => s.BranchId == request.BranchId.Value);
        if (request.ClientId.HasValue)
            query = query.Where(s => s.ClientId == request.ClientId.Value);
        if (request.CashierId.HasValue)
            query = query.Where(s => s.CashierId == request.CashierId.Value);
        if (request.PaymentMethod.HasValue)
            query = query.Where(s => s.PaymentMethod == request.PaymentMethod.Value);
        if (request.FromDate.HasValue)
            query = query.Where(s => s.SaleDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(s => s.SaleDate <= request.ToDate.Value);

        var sales = await query.OrderByDescending(s => s.SaleDate).ToListAsync(cancellationToken);

        return sales.Select(s => new SaleDto
        {
            Id = s.Id,
            TenantId = s.TenantId,
            SaleNumber = s.SaleNumber,
            BranchId = s.BranchId,
            BranchName = s.Branch.Name,
            ClientId = s.ClientId,
            ClientName = s.Client?.Email,
            CashierId = s.CashierId,
            CashierName = s.Cashier?.Email,
            SaleDate = s.SaleDate,
            Subtotal = s.Subtotal,
            TaxAmount = s.TaxAmount,
            DiscountAmount = s.DiscountAmount,
            Total = s.Total,
            PaymentMethod = s.PaymentMethod,
            InvoiceId = s.InvoiceId,
            InvoiceNumber = s.Invoice?.InvoiceNumber,
            Notes = s.Notes,
            Items = s.Items.Select(i => new SaleItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TaxRate = i.TaxRate,
                DiscountAmount = i.DiscountAmount,
                LineTotal = i.LineTotal
            }).ToList()
        }).ToList();
    }
}
