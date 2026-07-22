using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Sales.Commands.CheckoutSale;

public class CheckoutSaleCommandHandler : IRequestHandler<CheckoutSaleCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICommissionService _commissionService;

    public CheckoutSaleCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        ICommissionService commissionService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _commissionService = commissionService;
    }

    public async Task<Guid> Handle(CheckoutSaleCommand request, CancellationToken cancellationToken)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new DomainException("Sale must have at least one item");

        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;

        var branchExists = await _context.Branches.AnyAsync(b => b.Id == request.BranchId && b.TenantId == tenantId, cancellationToken);
        if (!branchExists)
            throw new NotFoundException("Branch", request.BranchId);

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => p.TenantId == tenantId && productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count)
            throw new DomainException("One or more products not found");

        var stockItems = await _context.StockItems
            .Where(s => s.TenantId == tenantId && s.BranchId == request.BranchId && productIds.Contains(s.ProductId))
            .ToListAsync(cancellationToken);

        // Stock check
        foreach (var item in request.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            if (product.TrackStock)
            {
                var stock = stockItems.FirstOrDefault(s => s.ProductId == item.ProductId);
                var available = stock?.Quantity ?? 0;
                if (available < item.Quantity)
                    throw new DomainException($"Insufficient stock for '{product.Name}'. Available: {available}");
            }
        }

        Coupon? coupon = null;
        if (request.CouponId.HasValue)
        {
            coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Id == request.CouponId.Value && c.TenantId == tenantId, cancellationToken)
                ?? throw new NotFoundException("Coupon", request.CouponId.Value);

            if (!coupon.IsActive)
                throw new DomainException("Coupon is inactive");
            if (coupon.EndDate.HasValue && coupon.EndDate.Value < now)
                throw new DomainException("Coupon has expired");
            if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value)
                throw new DomainException("Coupon usage limit reached");
            if (coupon.ApplicableTo != CouponApplicability.All && coupon.ApplicableTo != CouponApplicability.Products)
                throw new DomainException("Coupon is not applicable to product sales");
            if (coupon.MaxUsesPerUser.HasValue && request.ClientId.HasValue)
            {
                var clientUses = await _context.CouponUsages.CountAsync(
                    u => u.CouponId == coupon.Id && u.UserId == request.ClientId.Value, cancellationToken);
                if (clientUses >= coupon.MaxUsesPerUser.Value)
                    throw new DomainException("This client has reached the usage limit for this coupon");
            }
        }

        // Products with no explicit tax rate fall back to the gym's default tax setting (if configured).
        var defaultTaxRate = await GetDefaultTaxRateAsync(tenantId, cancellationToken);

        decimal subtotal = 0;
        decimal tax = 0;
        decimal lineDiscounts = 0;

        var saleItems = new List<SaleItem>();

        foreach (var item in request.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            var unitPrice = item.UnitPriceOverride ?? product.SellingPrice;
            var effectiveTaxRate = product.TaxRate > 0 ? product.TaxRate : defaultTaxRate;
            var gross = item.Quantity * unitPrice;
            var net = gross - item.DiscountAmount;
            var lineTax = net * (effectiveTaxRate / 100m);
            var lineTotal = net + lineTax;

            subtotal += gross;
            lineDiscounts += item.DiscountAmount;
            tax += lineTax;

            saleItems.Add(new SaleItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                TaxRate = effectiveTaxRate,
                DiscountAmount = item.DiscountAmount,
                LineTotal = lineTotal
            });
        }

        decimal total = subtotal - lineDiscounts + tax;
        decimal totalDiscount = lineDiscounts + request.ExtraDiscount;
        total -= request.ExtraDiscount;

        if (coupon != null)
        {
            decimal couponDiscount = coupon.DiscountType == DiscountType.Percentage
                ? total * (coupon.DiscountValue / 100m)
                : coupon.DiscountValue;

            if (coupon.MaxDiscountAmount.HasValue && couponDiscount > coupon.MaxDiscountAmount.Value)
                couponDiscount = coupon.MaxDiscountAmount.Value;
            if (coupon.MinimumAmount.HasValue && total < coupon.MinimumAmount.Value)
                throw new DomainException($"Minimum sale amount for this coupon is {coupon.MinimumAmount.Value:C}");

            totalDiscount += couponDiscount;
            total -= couponDiscount;
        }

        if (total < 0) total = 0;

        Guid? cashierId = null;
        if (Guid.TryParse(_currentUserService.UserId, out var uid)) cashierId = uid;

        var saleNumber = await GenerateSaleNumberAsync(tenantId, cancellationToken);

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SaleNumber = saleNumber,
            BranchId = request.BranchId,
            ClientId = request.ClientId,
            CashierId = cashierId,
            SaleDate = now,
            Subtotal = subtotal,
            TaxAmount = tax,
            DiscountAmount = totalDiscount,
            Total = total,
            PaymentMethod = request.PaymentMethod,
            CouponId = coupon?.Id,
            Notes = request.Notes
        };

        foreach (var si in saleItems)
            si.SaleId = sale.Id;

        _context.Sales.Add(sale);
        foreach (var si in saleItems)
            _context.SaleItems.Add(si);

        // Stock movements + decrement
        foreach (var item in request.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            if (!product.TrackStock) continue;

            var stock = stockItems.FirstOrDefault(s => s.ProductId == item.ProductId);
            if (stock == null)
            {
                stock = new StockItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = item.ProductId,
                    BranchId = request.BranchId,
                    Quantity = 0
                };
                _context.StockItems.Add(stock);
            }
            stock.Quantity -= item.Quantity;
            stock.LastMovementAt = now;

            _context.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductId = item.ProductId,
                BranchId = request.BranchId,
                Type = StockMovementType.Out,
                Quantity = item.Quantity,
                QuantityAfter = stock.Quantity,
                Reason = "Sale",
                ReferenceType = "Sale",
                ReferenceId = sale.Id,
                MovedAt = now,
                MovedById = cashierId
            });
        }

        // Auto-create Invoice
        var invoiceNumber = await GenerateInvoiceNumberAsync(tenantId, cancellationToken);
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceNumber = invoiceNumber,
            ClientId = request.ClientId,
            BranchId = request.BranchId,
            IssueDate = now,
            Subtotal = subtotal,
            TaxAmount = tax,
            DiscountAmount = totalDiscount,
            Total = total,
            AmountPaid = total, // POS is paid immediately
            Status = InvoiceStatus.Paid,
            CouponId = coupon?.Id,
            Notes = $"Auto-generated from Sale {saleNumber}"
        };

        _context.Invoices.Add(invoice);

        foreach (var si in saleItems)
        {
            _context.InvoiceItems.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InvoiceId = invoice.Id,
                ItemType = InvoiceItemType.Product,
                ReferenceId = si.ProductId,
                Description = si.ProductName,
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                TaxRate = si.TaxRate,
                DiscountAmount = si.DiscountAmount,
                LineTotal = si.LineTotal
            });
        }

        // Create Payment
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = invoice.Id,
            BranchId = request.BranchId,
            ClientId = request.ClientId,
            Amount = total,
            Method = request.PaymentMethod,
            ReceivedAt = now,
            ReceivedById = cashierId,
            ReceiptNumber = saleNumber,
            Notes = $"Payment for Sale {saleNumber}"
        };
        _context.Payments.Add(payment);

        sale.InvoiceId = invoice.Id;
        sale.PaymentId = payment.Id;

        if (coupon != null)
        {
            coupon.UsedCount++;
            _context.CouponUsages.Add(new CouponUsage
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CouponId = coupon.Id,
                UserId = request.ClientId,
                InvoiceId = invoice.Id,
                UsedAt = now,
                DiscountApplied = totalDiscount - lineDiscounts - request.ExtraDiscount
            });
        }

        // Accrue a sales commission for the cashier (staged on the same transaction).
        await _commissionService.AccrueAsync(
            tenantId, cashierId, CommissionSourceType.ProductSale, total, sale.Id, now,
            $"Commission for Sale {saleNumber}", cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return sale.Id;
    }

    private async Task<decimal> GetDefaultTaxRateAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var setting = await _context.TaxSettings
            .Where(t => t.TenantId == tenantId && t.IsActive && t.IsDefault)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return setting?.Rate ?? 0m;
    }

    private Task<string> GenerateSaleNumberAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        // Count + 1 collides when two API instances checkout concurrently.
        return Task.FromResult($"SALE-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..39]);
    }

    private Task<string> GenerateInvoiceNumberAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult($"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..38]);
    }
}
