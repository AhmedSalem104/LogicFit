using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Payments.DTOs;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid? SubscriptionId { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public Guid? ClientId { get; set; }
    public string? ClientName { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string MethodName => Method.ToString();
    public DateTime ReceivedAt { get; set; }
    public string? ReceivedByName { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public string? ReferenceNumber { get; set; }
}
