namespace LogicFit.Application.Features.CoachClients.DTOs;

public class CoachClientDto
{
    public Guid Id { get; set; }
    public Guid CoachId { get; set; }
    public string CoachName { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public string? ClientEmail { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? UnassignedAt { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    // Client Stats
    public bool HasActiveSubscription { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public int WorkoutProgramsCount { get; set; }
    public int DietPlansCount { get; set; }
    public int WorkoutSessionsCount { get; set; }
    public DateTime? LastSessionDate { get; set; }
}

public class AssignClientToCoachDto
{
    public Guid? CoachId { get; set; }
    public Guid ClientId { get; set; }
    public string? Notes { get; set; }
}
