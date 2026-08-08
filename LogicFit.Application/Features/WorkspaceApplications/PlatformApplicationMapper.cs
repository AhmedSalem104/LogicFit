using System.Text.Json;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.WorkspaceApplications;

internal static class PlatformApplicationMapper
{
    public static PlatformApplicationDto ToDto(
        ApplicationRequest application,
        string applicantEmail,
        string? applicantPhoneNumber,
        PlatformApplicationLifecycleDto? lifecycle = null) => new()
    {
        Id = application.Id,
        ApplicationType = application.ApplicationType,
        Status = application.Status,
        ApplicationStatus = application.Status,
        ApplicantEmail = applicantEmail,
        ApplicantPhoneNumber = applicantPhoneNumber,
        WorkspaceIdentifier = application.ReservedWorkspaceIdentifier,
        RequestedRole = application.RequestedRole,
        InformationRequest = application.InformationRequest,
        RequestedFields = string.IsNullOrWhiteSpace(application.RequestedFieldsJson)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(application.RequestedFieldsJson) ?? Array.Empty<string>(),
        DecisionReason = application.DecisionReason,
        SubmittedAt = application.SubmittedAt,
        ReviewedAt = application.ReviewedAt,
        ReviewedBy = application.ReviewedBy,
        ProvisionedWorkspaceId = application.ProvisionedWorkspaceId,
        WorkspaceType = lifecycle?.WorkspaceType,
        PaymentRequestId = lifecycle?.PaymentRequestId,
        PaymentStatus = lifecycle?.PaymentStatus,
        WorkspaceStatus = lifecycle?.WorkspaceStatus,
        SubscriptionStatus = lifecycle?.SubscriptionStatus,
        DatabaseStatus = lifecycle?.DatabaseStatus,
        DatabaseStatusCode = lifecycle?.DatabaseStatusCode,
        ProvisioningStatus = lifecycle?.ProvisioningStatus,
        UserJourneyStage = lifecycle?.UserJourneyStage ?? "Submitted",
        CanAccessDashboard = lifecycle?.CanAccessDashboard ?? false,
        RequiredAction = lifecycle?.RequiredAction,
        NextStep = lifecycle?.NextStep,
        UserMessage = lifecycle?.UserMessage,
        LastUpdatedAtUtc = lifecycle?.LastUpdatedAtUtc,
        ProvisioningErrorCode = lifecycle?.ProvisioningErrorCode,
        RowVersion = Convert.ToBase64String(application.RowVersion)
    };
}

public sealed class PlatformApplicationLifecycleDto
{
    public WorkspaceType? WorkspaceType { get; init; }
    public Guid? PaymentRequestId { get; init; }
    public PaymentRequestStatus? PaymentStatus { get; init; }
    public TenantStatus? WorkspaceStatus { get; init; }
    public TenantSubscriptionStatus? SubscriptionStatus { get; init; }
    public DatabaseResourceStatus? DatabaseStatus { get; init; }
    public string? DatabaseStatusCode { get; init; }
    public ProvisioningJobStatus? ProvisioningStatus { get; init; }
    public string UserJourneyStage { get; init; } = "Submitted";
    public bool CanAccessDashboard { get; init; }
    public string? RequiredAction { get; init; }
    public string? NextStep { get; init; }
    public string? UserMessage { get; init; }
    public DateTime? LastUpdatedAtUtc { get; init; }
    public string? ProvisioningErrorCode { get; init; }
}
