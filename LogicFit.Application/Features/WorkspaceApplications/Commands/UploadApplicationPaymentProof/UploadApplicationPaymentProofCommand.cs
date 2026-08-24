using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.UploadApplicationPaymentProof;

/// <summary>
/// Attaches a new payment-proof version to the application addressed by the short-lived tracking
/// token. The caller never supplies a payment-request id, which prevents cross-application uploads.
/// </summary>
public sealed class UploadApplicationPaymentProofCommand : IRequest<ApplicationPaymentProofUploadedDto>
{
    public string TrackingToken { get; init; } = string.Empty;
    public string ProofStorageKey { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string Sha256 { get; init; } = string.Empty;
}
