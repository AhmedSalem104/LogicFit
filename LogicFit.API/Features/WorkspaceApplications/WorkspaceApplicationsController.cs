using LogicFit.Application.Features.WorkspaceApplications.Commands.ResubmitApplication;
using LogicFit.Application.Features.WorkspaceApplications.Commands.SubmitFreelanceWorkspaceApplication;
using LogicFit.Application.Features.WorkspaceApplications.Commands.UpdateApplicationRequestedFields;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Application.Features.WorkspaceApplications.Queries.GetApplicationTrackingStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.WorkspaceApplications;

/// <summary>
/// Public onboarding surface for independently operated FreelanceCoach workspaces. It deliberately
/// uses an opaque, short-lived tracking token instead of issuing normal tenant authentication.
/// </summary>
[ApiController]
[Route("api/workspace-applications")]
[AllowAnonymous]
public sealed class WorkspaceApplicationsController : ControllerBase
{
    private const string TrackingTokenHeader = "X-Application-Tracking-Token";
    private readonly IMediator _mediator;

    public WorkspaceApplicationsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("freelance")]
    [ProducesResponseType(typeof(ApplicationTrackingSessionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApplicationTrackingSessionDto>> SubmitFreelance(
        [FromBody] SubmitFreelanceWorkspaceApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("tracking")]
    [ProducesResponseType(typeof(ApplicationTrackingStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationTrackingStatusDto>> GetTrackingStatus(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetApplicationTrackingStatusQuery(GetTrackingToken()), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("tracking/fields")]
    [ProducesResponseType(typeof(ApplicationTrackingStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationTrackingStatusDto>> UpdateRequestedFields(
        [FromBody] IReadOnlyDictionary<string, System.Text.Json.JsonElement> fields,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateApplicationRequestedFieldsCommand
        {
            TrackingToken = GetTrackingToken(),
            Fields = fields
        }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("tracking/resubmit")]
    [ProducesResponseType(typeof(ApplicationTrackingStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationTrackingStatusDto>> Resubmit(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResubmitApplicationCommand(GetTrackingToken()), cancellationToken);
        return Ok(result);
    }

    private string GetTrackingToken() => Request.Headers[TrackingTokenHeader].ToString();
}
