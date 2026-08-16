using LogicFit.Application.Features.Platform.PaymentRequests.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Platform.PaymentRequests.Commands.UploadPaymentProof;

/// <summary>Attaches a new private proof version before a manual payment is approved.</summary>
public sealed class UploadPaymentProofCommand : IRequest<PaymentRequestDto>
{
    public Guid PaymentRequestId { get; init; }
    public string ProofFileUrl { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string Sha256 { get; init; } = string.Empty;
}
