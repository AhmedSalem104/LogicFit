using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using MediatR;
using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.TenantBilling.Commands.SubmitPaymentRequest;

/// <summary>Owner submits a manual payment (proof already uploaded) for a chosen plan.</summary>
public class SubmitPaymentRequestCommand : IRequest<PaymentRequestDto>
{
    public Guid PlanId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public string? TransactionNumber { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? ProofFileUrl { get; set; }
    public string? Notes { get; set; }
    public PaymentRequestOperation Operation { get; set; } = PaymentRequestOperation.NewSubscription;
    public int? ExtensionDays { get; set; }
}
