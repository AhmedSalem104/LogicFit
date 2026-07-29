using LogicFit.Application.Common.Models;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.WorkspaceApplications.Queries.GetPlatformApplications;

public sealed class GetPlatformApplicationsQuery : IRequest<PagedResult<PlatformApplicationDto>>
{
    public ApplicationType? ApplicationType { get; init; }
    public ApplicationRequestStatus? Status { get; init; }
    public int Page { get; init; } = PageRequest.DefaultPageSize;
    public int PageSize { get; init; } = PageRequest.DefaultPageSize;
}
