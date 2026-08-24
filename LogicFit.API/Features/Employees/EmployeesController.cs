using LogicFit.Application.Features.Employees.Commands.CreateEmployee;
using LogicFit.Application.Features.Employees.Commands.TerminateEmployee;
using LogicFit.Application.Features.Employees.Commands.UpdateEmployee;
using LogicFit.Application.Features.Employees.DTOs;
using LogicFit.Application.Features.Employees.Queries.GetEmployees;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;
using LogicFit.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using LogicFit.Domain.Exceptions;

namespace LogicFit.API.Features.Employees;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageEmployees)]
[Authorize(Policy = WorkspaceCapabilities.GymStaff)]
public class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTime;
    public EmployeesController(IMediator mediator, IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTime)
    { _mediator = mediator; _context = context; _tenantService = tenantService; _dateTime = dateTime; }

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

    [HttpPost("{id}/qr/regenerate")]
    public async Task<ActionResult<object>> RegenerateQr(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var employee = await _context.EmployeeProfiles.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Employee", id);
        employee.QrCode = $"staff:{tenantId:N}:{Guid.NewGuid():N}";
        employee.QrGeneratedAt = _dateTime.UtcNow;
        employee.QrRevokedAt = null;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { employee.Id, employee.QrCode, employee.QrGeneratedAt });
    }

    [HttpPost("{id}/qr/revoke")]
    public async Task<IActionResult> RevokeQr(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var employee = await _context.EmployeeProfiles.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Employee", id);
        employee.QrRevokedAt = _dateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
