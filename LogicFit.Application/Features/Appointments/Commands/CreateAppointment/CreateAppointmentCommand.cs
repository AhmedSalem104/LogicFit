using MediatR;

namespace LogicFit.Application.Features.Appointments.Commands.CreateAppointment;

public class CreateAppointmentCommand : IRequest<Guid>
{
    public Guid? CoachId { get; set; }
    public Guid ClientId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
}
