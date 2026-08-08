using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.CreatePlatformWorkspaceApplication;

/// <summary>
/// Platform-admin entry point for the shared Gym/FreelanceCoach onboarding pipeline.
/// It creates an application and pending payment record; approval and provisioning remain
/// explicit follow-up steps.
/// </summary>
public sealed class CreatePlatformWorkspaceApplicationCommand : IRequest<DTOs.PlatformWorkspaceApplicationCreatedDto>
{
    public WorkspaceType WorkspaceType { get; init; }
    public string WorkspaceName { get; init; } = string.Empty;
    public string WorkspaceIdentifier { get; init; } = string.Empty;
    public string OwnerFullName { get; init; } = string.Empty;
    public string OwnerEmail { get; init; } = string.Empty;
    public string? OwnerPhoneNumber { get; init; }
    public Guid PlanId { get; init; }
    public BillingCycle BillingCycle { get; init; } = BillingCycle.Monthly;
    public string? BrandName { get; init; }
    public string? Description { get; init; }
    public string? Address { get; init; }
    public string? Specialization { get; init; }
    public string? DeliveryMode { get; init; }
}
