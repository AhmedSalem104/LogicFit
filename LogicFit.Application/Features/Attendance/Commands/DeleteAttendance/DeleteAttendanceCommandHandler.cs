using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Attendance.Commands.DeleteAttendance;

public class DeleteAttendanceCommandHandler : IRequestHandler<DeleteAttendanceCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteAttendanceCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(DeleteAttendanceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.TenantId == tenantId && !a.IsDeleted, cancellationToken);

        if (attendance == null)
            throw new NotFoundException("Attendance", request.Id);

        attendance.IsDeleted = true;
        attendance.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
