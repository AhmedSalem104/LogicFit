using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Invoices.Commands.CreateInvoice;

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public CreateInvoiceCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Guid> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new DomainException("Invoice must have at least one item");

        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;

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
        }

        decimal subtotal = 0;
        decimal tax = 0;
        decimal discount = 0;
        var items = new List<InvoiceItem>();

        foreach (var i in request.Items)
        {
            var gross = i.Quantity * i.UnitPrice;
            var lineDiscount = i.DiscountAmount;
            var net = gross - lineDiscount;
            var lineTax = net * (i.TaxRate / 100m);
            var lineTotal = net + lineTax;

            subtotal += gross;
            discount += lineDiscount;
            tax += lineTax;

            items.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ItemType = i.ItemType,
                ReferenceId = i.ReferenceId,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TaxRate = i.TaxRate,
                DiscountAmount = lineDiscount,
                LineTotal = lineTotal
            });
        }

        decimal total = subtotal - discount + tax;

        if (coupon != null)
        {
            decimal couponDiscount = coupon.DiscountType == DiscountType.Percentage
                ? total * (coupon.DiscountValue / 100m)
                : coupon.DiscountValue;

            if (coupon.MaxDiscountAmount.HasValue && couponDiscount > coupon.MaxDiscountAmount.Value)
                couponDiscount = coupon.MaxDiscountAmount.Value;

            if (coupon.MinimumAmount.HasValue && total < coupon.MinimumAmount.Value)
                throw new DomainException($"Minimum invoice amount for this coupon is {coupon.MinimumAmount.Value:C}");

            discount += couponDiscount;
            total -= couponDiscount;
        }

        if (total < 0) total = 0;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceNumber = await GenerateInvoiceNumberAsync(tenantId, cancellationToken),
            ClientId = request.ClientId,
            BranchId = request.BranchId,
            IssueDate = request.IssueDate ?? now,
            DueDate = request.DueDate,
            Subtotal = subtotal,
            TaxAmount = tax,
            DiscountAmount = discount,
            Total = total,
            AmountPaid = 0,
            Status = request.IssueImmediately ? InvoiceStatus.Issued : InvoiceStatus.Draft,
            CouponId = coupon?.Id,
            Notes = request.Notes
        };

        foreach (var item in items)
            item.InvoiceId = invoice.Id;

        _context.Invoices.Add(invoice);
        foreach (var item in items)
            _context.InvoiceItems.Add(item);

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
                DiscountApplied = coupon.DiscountType == DiscountType.Percentage
                    ? (subtotal - discount + tax) * (coupon.DiscountValue / 100m)
                    : coupon.DiscountValue
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }

    private async Task<string> GenerateInvoiceNumberAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";
        var count = await _context.Invoices
            .Where(i => i.TenantId == tenantId && i.InvoiceNumber.StartsWith(prefix))
            .CountAsync(cancellationToken);
        return $"{prefix}{(count + 1):D6}";
    }
}
