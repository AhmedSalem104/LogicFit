using MediatR;

namespace LogicFit.Application.Features.Appointments.Commands.DeleteAppointment;

public class DeleteAppointmentCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
