using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Invoices.Commands.CreateInvoice;

public class CreateInvoiceCommand : IRequest<Guid>
{
    public Guid? ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? CouponId { get; set; }
    public string? Notes { get; set; }
    public List<CreateInvoiceItem> Items { get; set; } = new();
    public bool IssueImmediately { get; set; } = true;
}

public class CreateInvoiceItem
{
    public InvoiceItemType ItemType { get; set; } = InvoiceItemType.Manual;
    public Guid? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal DiscountAmount { get; set; }
}
