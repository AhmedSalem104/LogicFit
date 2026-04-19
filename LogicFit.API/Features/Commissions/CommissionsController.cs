using LogicFit.Application.Features.Commissions.Commands.CreateCommissionRule;
using LogicFit.Application.Features.Commissions.DTOs;
using LogicFit.Application.Features.Commissions.Queries.GetCommissionRules;
using LogicFit.Application.Features.Commissions.Queries.GetCommissions;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Commissions;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public CommissionsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<CommissionDto>>> Get(
        [FromQuery] Guid? employeeId,
        [FromQuery] CommissionStatus? status,
        [FromQuery] CommissionSourceType? sourceType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
        => Ok(await _mediator.Send(new GetCommissionsQuery
        {
            EmployeeId = employeeId,
            Status = status,
            SourceType = sourceType,
            FromDate = fromDate,
            ToDate = toDate
        }));

    [HttpGet("rules")]
    public async Task<ActionResult<List<CommissionRuleDto>>> GetRules([FromQuery] bool? isActive)
        => Ok(await _mediator.Send(new GetCommissionRulesQuery { IsActive = isActive }));

    [HttpPost("rules")]
    public async Task<ActionResult<Guid>> CreateRule(CreateCommissionRuleCommand command)
        => Ok(await _mediator.Send(command));
}
