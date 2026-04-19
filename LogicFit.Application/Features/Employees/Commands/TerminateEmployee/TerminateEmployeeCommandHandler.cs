using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Employees.Commands.TerminateEmployee;

public class TerminateEmployeeCommandHandler : IRequestHandler<TerminateEmployeeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public TerminateEmployeeCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(TerminateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var employee = await _context.EmployeeProfiles
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Employee", request.Id);

        employee.TerminationDate = request.TerminationDate ?? _dateTimeService.UtcNow;
        employee.Notes = (employee.Notes ?? string.Empty) + $"\n[Terminated] {request.Reason}";

        await _context.SaveChangesAsync(cancellationToken);
    }
}
