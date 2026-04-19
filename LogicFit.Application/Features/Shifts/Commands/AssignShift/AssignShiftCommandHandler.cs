using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Shifts.Commands.AssignShift;

public class AssignShiftCommandHandler : IRequestHandler<AssignShiftCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public AssignShiftCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(AssignShiftCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var shiftExists = await _context.Shifts.AnyAsync(s => s.Id == request.ShiftId && s.TenantId == tenantId, cancellationToken);
        if (!shiftExists) throw new NotFoundException("Shift", request.ShiftId);

        var employeeExists = await _context.EmployeeProfiles.AnyAsync(e => e.Id == request.EmployeeId && e.TenantId == tenantId, cancellationToken);
        if (!employeeExists) throw new NotFoundException("Employee", request.EmployeeId);

        var date = request.Date.Date;
        var duplicate = await _context.ShiftAssignments.AnyAsync(a =>
            a.EmployeeId == request.EmployeeId && a.ShiftId == request.ShiftId && a.Date == date && a.TenantId == tenantId, cancellationToken);
        if (duplicate) throw new ConflictException("Shift already assigned to employee on this date");

        var assignment = new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ShiftId = request.ShiftId,
            EmployeeId = request.EmployeeId,
            Date = date,
            Notes = request.Notes
        };

        _context.ShiftAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);
        return assignment.Id;
    }
}
