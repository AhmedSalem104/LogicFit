using LogicFit.Application.Features.Appointments.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Appointments.Queries.GetAppointmentById;

public class GetAppointmentByIdQuery : IRequest<AppointmentDto>
{
    public Guid Id { get; set; }
}
