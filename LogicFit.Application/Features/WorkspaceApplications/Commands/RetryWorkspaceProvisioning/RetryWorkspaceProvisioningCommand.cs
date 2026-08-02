using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Commands.RetryWorkspaceProvisioning;

public sealed record RetryWorkspaceProvisioningCommand(Guid ApplicationId) : IRequest<PlatformApplicationDto>;
