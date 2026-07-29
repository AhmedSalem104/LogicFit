using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Identity.Commands.ReissueApplicationTrackingSessions;

public sealed record ReissueApplicationTrackingSessionsCommand(string WorkspaceSelectionToken)
    : IRequest<IReadOnlyList<ApplicationTrackingSessionDto>>;
