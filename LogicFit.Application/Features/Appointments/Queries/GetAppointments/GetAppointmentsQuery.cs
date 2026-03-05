using LogicFit.Application.Features.Appointments.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Appointments.Queries.GetAppointments;

public class GetAppointmentsQuery : IRequest<List<AppointmentDto>>
{
    public Guid? CoachId { get; set; }
    public Guid? ClientId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public AppointmentStatus? Status { get; set; }
}
