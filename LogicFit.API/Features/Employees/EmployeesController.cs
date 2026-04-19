using LogicFit.Application.Features.Employees.Commands.CreateEmployee;
using LogicFit.Application.Features.Employees.Commands.TerminateEmployee;
using LogicFit.Application.Features.Employees.Commands.UpdateEmployee;
using LogicFit.Application.Features.Employees.DTOs;
using LogicFit.Application.Features.Employees.Queries.GetEmployees;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Employees;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;
    public EmployeesController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> Get(
        [FromQuery] Guid? branchId,
        [FromQuery] string? department,
        [FromQuery] bool? isActive,
        [FromQuery] string? searchTerm)
        => Ok(await _mediator.Send(new GetEmployeesQuery
        {
            BranchId = branchId,
            Department = department,
            IsActive = isActive,
            SearchTerm = searchTerm
        }));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateEmployeeCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateEmployeeCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{id}/terminate")]
    public async Task<ActionResult> Terminate(Guid id, [FromBody] TerminateEmployeeCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }
}
