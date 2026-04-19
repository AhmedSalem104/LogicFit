using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Leaves.Commands.CreateLeaveRequest;

public class CreateLeaveRequestCommandHandler : IRequestHandler<CreateLeaveRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateLeaveRequestCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        if (request.ToDate < request.FromDate)
            throw new DomainException("ToDate must be on or after FromDate");

        var tenantId = _tenantService.GetCurrentTenantId();
        var employeeExists = await _context.EmployeeProfiles.AnyAsync(e => e.Id == request.EmployeeId && e.TenantId == tenantId, cancellationToken);
        if (!employeeExists) throw new NotFoundException("Employee", request.EmployeeId);

        var leave = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = request.EmployeeId,
            FromDate = request.FromDate.Date,
            ToDate = request.ToDate.Date,
            LeaveType = request.LeaveType,
            Reason = request.Reason
        };
        _context.LeaveRequests.Add(leave);
        await _context.SaveChangesAsync(cancellationToken);
        return leave.Id;
    }
}
