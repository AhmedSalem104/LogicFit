using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.ClientDashboard.DTOs;

public class ClientDashboardDto
{
    public List<MyWorkoutProgramDto> ActivePrograms { get; set; } = new();
    public List<MyDietPlanDto> ActiveDietPlans { get; set; } = new();
    public MySubscriptionSummaryDto? ActiveSubscription { get; set; }
    public List<MyBodyMeasurementDto> RecentMeasurements { get; set; } = new();
    public MyCoachDto? AssignedCoach { get; set; }
    public int UnreadNotificationCount { get; set; }
}

public class MyWorkoutProgramDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CoachName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class MyDietPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CoachName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public PlanStatus Status { get; set; }
    public double TargetCalories { get; set; }
    public double TargetProtein { get; set; }
    public double TargetCarbs { get; set; }
    public double TargetFats { get; set; }
}

public class MySubscriptionSummaryDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
}

public class MyBodyMeasurementDto
{
    public Guid Id { get; set; }
    public DateTime DateRecorded { get; set; }
    public double? WeightKg { get; set; }
    public double? BodyFatPercent { get; set; }
    public double? SkeletalMuscleMass { get; set; }
}

public class MyCoachDto
{
    public Guid CoachId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime AssignedAt { get; set; }
}
