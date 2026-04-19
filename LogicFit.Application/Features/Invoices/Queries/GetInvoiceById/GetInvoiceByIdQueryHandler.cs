using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Invoices.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Invoices.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetInvoiceByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<InvoiceDto?> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var i = await _context.Invoices
            .Include(x => x.Client)
            .Include(x => x.Branch)
            .Include(x => x.Coupon)
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == tenantId, cancellationToken);

        if (i == null) return null;

        return new InvoiceDto
        {
            Id = i.Id,
            TenantId = i.TenantId,
            InvoiceNumber = i.InvoiceNumber,
            ClientId = i.ClientId,
            ClientName = i.Client?.Email,
            BranchId = i.BranchId,
            BranchName = i.Branch?.Name,
            IssueDate = i.IssueDate,
            DueDate = i.DueDate,
            Subtotal = i.Subtotal,
            TaxAmount = i.TaxAmount,
            DiscountAmount = i.DiscountAmount,
            Total = i.Total,
            AmountPaid = i.AmountPaid,
            RemainingAmount = i.RemainingAmount,
            Status = i.Status,
            CouponId = i.CouponId,
            CouponCode = i.Coupon?.Code,
            Notes = i.Notes,
            PdfUrl = i.PdfUrl,
            Items = i.Items.Select(it => new InvoiceItemDto
            {
                Id = it.Id,
                ItemType = it.ItemType,
                ReferenceId = it.ReferenceId,
                Description = it.Description,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice,
                TaxRate = it.TaxRate,
                DiscountAmount = it.DiscountAmount,
                LineTotal = it.LineTotal
            }).ToList(),
            Payments = i.Payments.Select(p => new InvoicePaymentDto
            {
                Id = p.Id,
                Amount = p.Amount,
                Method = p.Method,
                ReceivedAt = p.ReceivedAt,
                ReceiptNumber = p.ReceiptNumber
            }).ToList()
        };
    }
}
