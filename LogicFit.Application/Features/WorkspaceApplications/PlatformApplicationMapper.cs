using System.Text.Json;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Entities;

namespace LogicFit.Application.Features.WorkspaceApplications;

internal static class PlatformApplicationMapper
{
    public static PlatformApplicationDto ToDto(ApplicationRequest application, string applicantEmail, string? applicantPhoneNumber) => new()
    {
        Id = application.Id,
        ApplicationType = application.ApplicationType,
        Status = application.Status,
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
        RowVersion = Convert.ToBase64String(application.RowVersion)
    };
}
