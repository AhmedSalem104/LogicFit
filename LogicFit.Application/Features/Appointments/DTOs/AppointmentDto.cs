using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Appointments.DTOs;

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid CoachId { get; set; }
    public string? CoachName { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public AppointmentStatus Status { get; set; }
}
