using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Platform.PaymentRequests.DTOs;

public class PaymentRequestDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? TenantName { get; set; }
    public Guid PlanId { get; set; }
    public string? PlanName { get; set; }
    public Guid? TenantSubscriptionId { get; set; }
    public PaymentRequestOperation Operation { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public Guid? PaymentMethodId { get; set; }
    public string? TransactionNumber { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? ProofFileUrl { get; set; }
    public string? Notes { get; set; }
    public PaymentRequestStatus Status { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
