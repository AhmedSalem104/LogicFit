using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Invoices.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Invoices.Queries.GetInvoices;

public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, List<InvoiceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetInvoicesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<InvoiceDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Branch)
            .Include(i => i.Coupon)
            .Where(i => i.TenantId == tenantId)
            .AsQueryable();

        if (request.ClientId.HasValue)
            query = query.Where(i => i.ClientId == request.ClientId.Value);
        if (request.BranchId.HasValue)
            query = query.Where(i => i.BranchId == request.BranchId.Value);
        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);
        if (request.FromDate.HasValue)
            query = query.Where(i => i.IssueDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(i => i.IssueDate <= request.ToDate.Value);

        var invoices = await query.OrderByDescending(i => i.IssueDate).ToListAsync(cancellationToken);

        return invoices.Select(i => new InvoiceDto
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
            PdfUrl = i.PdfUrl
        }).ToList();
    }
}
