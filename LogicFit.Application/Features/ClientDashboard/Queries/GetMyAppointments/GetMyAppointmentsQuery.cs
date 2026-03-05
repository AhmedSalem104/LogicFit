using MediatR;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMyAppointments;

public class GetMyAppointmentsQuery : IRequest<List<MyAppointmentDto>>
{
}

public class MyAppointmentDto
{
    public Guid Id { get; set; }
    public string? CoachName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Title { get; set; }
    public string Status { get; set; } = string.Empty;
}
