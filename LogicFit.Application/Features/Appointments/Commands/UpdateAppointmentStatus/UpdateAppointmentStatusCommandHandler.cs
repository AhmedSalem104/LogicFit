using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Appointments.Commands.UpdateAppointmentStatus;

public class UpdateAppointmentStatusCommandHandler : IRequestHandler<UpdateAppointmentStatusCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateAppointmentStatusCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.TenantId == tenantId && !a.IsDeleted, cancellationToken);

        if (appointment == null)
            throw new NotFoundException("Appointment", request.Id);

        appointment.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
