using LogicFit.Application.Features.WorkspaceApplications.Commands.SponsorFreelanceMembership;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using LogicFit.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.WorkspaceApplications;

[ApiController]
[Route("api/freelance/team/applications")]
[Authorize(Policy = Permissions.ManageCoaches)]
public sealed class FreelanceTeamApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FreelanceTeamApplicationsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(ApplicationTrackingStatusDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApplicationTrackingStatusDto>> Sponsor(
        [FromBody] SponsorFreelanceMembershipCommand command,
        CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _mediator.Send(command, cancellationToken));
}
