using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.TenantBilling.DTOs;

public class SubscriptionInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public SubscriptionInvoiceStatus Status { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
}
