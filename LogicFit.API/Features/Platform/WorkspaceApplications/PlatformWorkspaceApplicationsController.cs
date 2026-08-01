using LogicFit.Application.Common.Models;
using LogicFit.Application.Features.WorkspaceApplications.Commands.ApproveFreelanceWorkspaceApplication;
using LogicFit.Application.Features.WorkspaceApplications.Commands.ApproveMembershipApplication;
using LogicFit.Application.Features.WorkspaceApplications.Commands.RejectApplication;
using LogicFit.Application.Features.WorkspaceApplications.Commands.RequestApplicationInformation;
using LogicFit.Application.Features.WorkspaceApplications.Commands.StartApplicationReview;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Application.Features.WorkspaceApplications.Queries.GetPlatformApplications;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Platform.WorkspaceApplications;

/// <summary>
/// Platform review surface for workspace-creation and freelance membership applications.
/// This controller belongs to the unified API host so its existing contract is published and
/// included in the generated endpoint catalog.
/// </summary>
[ApiController]
[Route("api/platform/workspace-applications")]
[Authorize(Policy = Permissions.ManageTenants)]
public sealed class PlatformWorkspaceApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformWorkspaceApplicationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PlatformApplicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PlatformApplicationDto>?>> List(
        [FromQuery] ApplicationType? applicationType,
        [FromQuery] ApplicationRequestStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetPlatformApplicationsQuery
        {
            ApplicationType = applicationType,
            Status = status,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpPost("{id:guid}/start-review")]
    [Authorize(Policy = OtpStepUpRequirement.PolicyName)]
    public async Task<ActionResult<PlatformApplicationDto>> StartReview(
        Guid id,
        [FromBody] ConcurrencyRequest request,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new StartApplicationReviewCommand(id, request.RowVersion), cancellationToken));

    [HttpPost("{id:guid}/request-information")]
    [Authorize(Policy = OtpStepUpRequirement.PolicyName)]
    public async Task<ActionResult<PlatformApplicationDto>> RequestInformation(
        Guid id,
        [FromBody] RequestInformationRequest request,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new RequestApplicationInformationCommand
        {
            ApplicationId = id,
            RowVersion = request.RowVersion,
            Message = request.Message,
            RequestedFields = request.RequestedFields
        }, cancellationToken));

    [HttpPost("{id:guid}/approve-freelance")]
    [Authorize(Policy = OtpStepUpRequirement.PolicyName)]
    public async Task<ActionResult<PlatformApplicationDto>> ApproveFreelance(
        Guid id,
        [FromBody] ConcurrencyRequest request,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ApproveFreelanceWorkspaceApplicationCommand(id, request.RowVersion), cancellationToken));

    [HttpPost("{id:guid}/approve-membership")]
    [Authorize(Policy = OtpStepUpRequirement.PolicyName)]
    public async Task<ActionResult<PlatformApplicationDto>> ApproveMembership(
        Guid id,
        [FromBody] ConcurrencyRequest request,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ApproveMembershipApplicationCommand(id, request.RowVersion), cancellationToken));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = OtpStepUpRequirement.PolicyName)]
    public async Task<ActionResult<PlatformApplicationDto>> Reject(
        Guid id,
        [FromBody] RejectRequest request,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new RejectApplicationCommand
        {
            ApplicationId = id,
            RowVersion = request.RowVersion,
            Reason = request.Reason
        }, cancellationToken));

    public class ConcurrencyRequest
    {
        public string RowVersion { get; init; } = string.Empty;
    }

    public sealed class RequestInformationRequest : ConcurrencyRequest
    {
        public string Message { get; init; } = string.Empty;
        public IReadOnlyList<string> RequestedFields { get; init; } = Array.Empty<string>();
    }

    public sealed class RejectRequest : ConcurrencyRequest
    {
        public string Reason { get; init; } = string.Empty;
    }
}
