using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Attendance.Commands.CheckOut;

public class CheckOutCommandHandler : IRequestHandler<CheckOutCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CheckOutCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(CheckOutCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.Id == request.AttendanceId && a.TenantId == tenantId && !a.IsDeleted, cancellationToken);

        if (attendance == null)
            throw new NotFoundException("Attendance", request.AttendanceId);

        if (attendance.CheckOutTime.HasValue)
            throw new ConflictException("This attendance record has already been checked out.");

        attendance.CheckOutTime = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
