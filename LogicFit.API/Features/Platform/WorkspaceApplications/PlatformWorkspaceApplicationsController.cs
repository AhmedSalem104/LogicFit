using LogicFit.Application.Common.Models;
using LogicFit.Application.Features.WorkspaceApplications.Commands.ApproveFreelanceWorkspaceApplication;
using LogicFit.Application.Features.WorkspaceApplications.Commands.CreatePlatformWorkspaceApplication;
using LogicFit.Application.Features.WorkspaceApplications.Commands.ApproveMembershipApplication;
using LogicFit.Application.Features.WorkspaceApplications.Commands.RejectApplication;
using LogicFit.Application.Features.WorkspaceApplications.Commands.RequestApplicationInformation;
using LogicFit.Application.Features.WorkspaceApplications.Commands.RetryWorkspaceProvisioning;
using LogicFit.Application.Features.WorkspaceApplications.Commands.StartApplicationReview;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Application.Features.WorkspaceApplications.Queries.GetPlatformApplications;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
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
        [FromQuery] PaymentRequestStatus? paymentStatus,
        [FromQuery] TenantStatus? workspaceStatus,
        [FromQuery] TenantSubscriptionStatus? subscriptionStatus,
        [FromQuery] ProvisioningJobStatus? provisioningStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetPlatformApplicationsQuery
        {
            ApplicationType = applicationType,
            Status = status,
            PaymentStatus = paymentStatus,
            WorkspaceStatus = workspaceStatus,
            SubscriptionStatus = subscriptionStatus,
            ProvisioningStatus = provisioningStatus,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(PlatformWorkspaceApplicationCreatedDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PlatformWorkspaceApplicationCreatedDto>> Create(
        [FromBody] CreatePlatformWorkspaceApplicationCommand command,
        CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _mediator.Send(command, cancellationToken));

    [HttpPost("{id:guid}/start-review")]
    public async Task<ActionResult<PlatformApplicationDto>> StartReview(
        Guid id,
        [FromBody] ConcurrencyRequest request,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new StartApplicationReviewCommand(id, request.RowVersion), cancellationToken));

    [HttpPost("{id:guid}/request-information")]
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

    [HttpPost("{id:guid}/approve-workspace")]
    public async Task<ActionResult<PlatformApplicationDto>> ApproveWorkspace(
        Guid id,
        [FromBody] ConcurrencyRequest request,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ApproveFreelanceWorkspaceApplicationCommand(id, request.RowVersion), cancellationToken));

    [HttpPost("{id:guid}/approve-freelance")]
    [Obsolete("Use approve-workspace for both Gym and FreelanceCoach applications.")]
    public Task<ActionResult<PlatformApplicationDto>> ApproveFreelance(
        Guid id,
        [FromBody] ConcurrencyRequest request,
        CancellationToken cancellationToken)
        => ApproveWorkspace(id, request, cancellationToken);

    [HttpPost("{id:guid}/approve-membership")]
    public async Task<ActionResult<PlatformApplicationDto>> ApproveMembership(
        Guid id,
        [FromBody] ConcurrencyRequest request,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ApproveMembershipApplicationCommand(id, request.RowVersion), cancellationToken));

    [HttpPost("{id:guid}/retry-provisioning")]
    public async Task<ActionResult<PlatformApplicationDto>> RetryProvisioning(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new RetryWorkspaceProvisioningCommand(id), cancellationToken));

    [HttpPost("{id:guid}/reject")]
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
