using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Queries.GetApplicationTrackingStatus;

public sealed record GetApplicationTrackingStatusQuery(string TrackingToken) : IRequest<ApplicationTrackingStatusDto>;
