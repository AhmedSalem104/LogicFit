using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Appointments.Commands.DeleteAppointment;

public class DeleteAppointmentCommandHandler : IRequestHandler<DeleteAppointmentCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteAppointmentCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(DeleteAppointmentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.TenantId == tenantId && !a.IsDeleted, cancellationToken);

        if (appointment == null)
            throw new NotFoundException("Appointment", request.Id);

        appointment.IsDeleted = true;
        appointment.DeletedAt = DateTime.UtcNow;
        appointment.DeletedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
