using LogicFit.Application.Features.Payroll.Commands.ApprovePayroll;
using LogicFit.Application.Features.Payroll.Commands.GeneratePayroll;
using LogicFit.Application.Features.Payroll.Commands.PayPayroll;
using LogicFit.Application.Features.Payroll.Commands.UpdatePayrollItem;
using LogicFit.Application.Features.Payroll.DTOs;
using LogicFit.Application.Features.Payroll.Queries.GetPayrollRuns;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Payroll;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IMediator _mediator;
    public PayrollController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<PayrollRunDto>>> Get(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? branchId,
        [FromQuery] PayrollStatus? status)
        => Ok(await _mediator.Send(new GetPayrollRunsQuery { Year = year, Month = month, BranchId = branchId, Status = status }));

    [HttpPost("generate")]
    public async Task<ActionResult<Guid>> Generate(GeneratePayrollCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("items/{id}")]
    public async Task<ActionResult> UpdateItem(Guid id, UpdatePayrollItemCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult> Approve(Guid id)
    {
        await _mediator.Send(new ApprovePayrollCommand { Id = id });
        return NoContent();
    }

    [HttpPost("{id}/pay")]
    public async Task<ActionResult> Pay(Guid id)
    {
        await _mediator.Send(new PayPayrollCommand { Id = id });
        return NoContent();
    }
}
