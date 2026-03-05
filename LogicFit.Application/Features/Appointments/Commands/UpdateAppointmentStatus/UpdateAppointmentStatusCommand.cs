using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Appointments.Commands.UpdateAppointmentStatus;

public class UpdateAppointmentStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public AppointmentStatus Status { get; set; }
}
