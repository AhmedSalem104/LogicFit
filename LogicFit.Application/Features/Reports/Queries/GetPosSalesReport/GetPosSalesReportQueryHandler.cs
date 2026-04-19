using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Reports.Queries.GetPosSalesReport;

public class GetPosSalesReportQueryHandler : IRequestHandler<GetPosSalesReportQuery, PosSalesReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public GetPosSalesReportQueryHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<PosSalesReportDto> Handle(GetPosSalesReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;
        var from = request.FromDate ?? new DateTime(now.Year, now.Month, 1);
        var to = request.ToDate ?? now;

        var salesQuery = _context.Sales
            .Include(s => s.Branch)
            .Include(s => s.Cashier)
            .Include(s => s.Items)
            .Where(s => s.TenantId == tenantId && s.SaleDate >= from && s.SaleDate <= to);

        if (request.BranchId.HasValue)
            salesQuery = salesQuery.Where(s => s.BranchId == request.BranchId.Value);

        var sales = await salesQuery.ToListAsync(cancellationToken);

        var allItems = sales.SelectMany(s => s.Items).ToList();

        var topProducts = allItems
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.LineTotal)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(Math.Max(1, request.TopProductsCount))
            .ToList();

        var byCashier = sales
            .GroupBy(s => new { s.CashierId, Name = s.Cashier?.Email ?? "(System)" })
            .Select(g => new SalesByCashierDto
            {
                CashierId = g.Key.CashierId,
                CashierName = g.Key.Name,
                SalesCount = g.Count(),
                Revenue = g.Sum(s => s.Total)
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();

        var byBranch = sales
            .GroupBy(s => new { s.BranchId, s.Branch.Name })
            .Select(g => new SalesByBranchDto
            {
                BranchId = g.Key.BranchId,
                BranchName = g.Key.Name,
                SalesCount = g.Count(),
                Revenue = g.Sum(s => s.Total)
            })
            .OrderByDescending(b => b.Revenue)
            .ToList();

        var byPayment = sales
            .GroupBy(s => s.PaymentMethod)
            .Select(g => new SalesByPaymentDto
            {
                PaymentMethod = g.Key.ToString(),
                Count = g.Count(),
                Revenue = g.Sum(s => s.Total)
            })
            .OrderByDescending(p => p.Revenue)
            .ToList();

        return new PosSalesReportDto
        {
            FromDate = from,
            ToDate = to,
            TotalRevenue = sales.Sum(s => s.Total),
            TotalTax = sales.Sum(s => s.TaxAmount),
            TotalDiscount = sales.Sum(s => s.DiscountAmount),
            SalesCount = sales.Count,
            ItemsSold = (int)allItems.Sum(i => i.Quantity),
            TopProducts = topProducts,
            ByCashier = byCashier,
            ByBranch = byBranch,
            ByPaymentMethod = byPayment
        };
    }
}
