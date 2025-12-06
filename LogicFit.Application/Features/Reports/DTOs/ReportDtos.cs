namespace LogicFit.Application.Features.Reports.DTOs;

// Dashboard Report
public class DashboardReportDto
{
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public int NewClientsThisMonth { get; set; }
    public int TotalCoaches { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int ExpiringSubscriptions { get; set; } // Expiring in 7 days
    public decimal TotalRevenueThisMonth { get; set; }
    public decimal TotalRevenueLastMonth { get; set; }
    public int TotalWorkoutsThisMonth { get; set; }
    public int TotalDietPlansActive { get; set; }
}

// Clients Report
public class ClientsReportDto
{
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public int InactiveClients { get; set; }
    public int NewClientsThisMonth { get; set; }
    public int ClientsWithActiveSubscription { get; set; }
    public int ClientsWithoutSubscription { get; set; }
    public List<ClientSummaryDto> TopClients { get; set; } = new();
    public List<MonthlyClientDto> MonthlyTrend { get; set; } = new();
}

public class ClientSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int TotalSessions { get; set; }
    public decimal TotalPaid { get; set; }
}

public class MonthlyClientDto
{
    public string Month { get; set; } = string.Empty;
    public int NewClients { get; set; }
    public int ChurnedClients { get; set; }
}

// Subscriptions Report
public class SubscriptionsReportDto
{
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int ExpiredSubscriptions { get; set; }
    public int CancelledSubscriptions { get; set; }
    public int ExpiringIn7Days { get; set; }
    public int ExpiringIn30Days { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public List<SubscriptionPlanStatsDto> PlanStatistics { get; set; } = new();
    public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
}

public class SubscriptionPlanStatsDto
{
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int ActiveCount { get; set; }
    public int TotalSold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class MonthlyRevenueDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int SubscriptionCount { get; set; }
}

// Workouts Report
public class WorkoutsReportDto
{
    public int TotalPrograms { get; set; }
    public int ActivePrograms { get; set; }
    public int TotalSessionsCompleted { get; set; }
    public int SessionsThisMonth { get; set; }
    public double AverageSessionsPerClient { get; set; }
    public List<ExercisePopularityDto> MostPopularExercises { get; set; } = new();
    public List<MuscleGroupStatsDto> MuscleGroupStats { get; set; } = new();
    public List<WorkoutTrendDto> WeeklyTrend { get; set; } = new();
}

public class ExercisePopularityDto
{
    public Guid ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

public class MuscleGroupStatsDto
{
    public string MuscleGroup { get; set; } = string.Empty;
    public int ExerciseCount { get; set; }
    public int SessionCount { get; set; }
}

public class WorkoutTrendDto
{
    public string Date { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public double TotalVolume { get; set; }
}

// Diet Plans Report
public class DietPlansReportDto
{
    public int TotalDietPlans { get; set; }
    public int ActiveDietPlans { get; set; }
    public int CompletedDietPlans { get; set; }
    public double AverageTargetCalories { get; set; }
    public int TotalMealLogs { get; set; }
    public int MealLogsThisMonth { get; set; }
    public double AverageComplianceRate { get; set; }
    public List<FoodPopularityDto> MostUsedFoods { get; set; } = new();
}

public class FoodPopularityDto
{
    public Guid FoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

// Coaches Report
public class CoachesReportDto
{
    public int TotalCoaches { get; set; }
    public int ActiveCoaches { get; set; }
    public List<CoachPerformanceDto> CoachPerformance { get; set; } = new();
}

public class CoachPerformanceDto
{
    public Guid CoachId { get; set; }
    public string CoachName { get; set; } = string.Empty;
    public int ClientsAssigned { get; set; }
    public int WorkoutProgramsCreated { get; set; }
    public int DietPlansCreated { get; set; }
    public decimal TotalRevenue { get; set; }
}

// Financial Report
public class FinancialReportDto
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueLastMonth { get; set; }
    public decimal GrowthPercentage { get; set; }
    public decimal AverageSubscriptionValue { get; set; }
    public decimal TotalWalletBalance { get; set; }
    public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
    public List<PaymentMethodStatsDto> PaymentMethods { get; set; } = new();
}

public class PaymentMethodStatsDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

// Body Measurements Report
public class BodyMeasurementsReportDto
{
    public int TotalMeasurements { get; set; }
    public int MeasurementsThisMonth { get; set; }
    public int ClientsWithMeasurements { get; set; }
    public double AverageWeightChange { get; set; }
    public List<MeasurementTrendDto> WeightTrend { get; set; } = new();
}

public class MeasurementTrendDto
{
    public string Month { get; set; } = string.Empty;
    public double AverageWeight { get; set; }
    public int MeasurementCount { get; set; }
}

// ===== Coach Reports =====

// Coach Dashboard Report
public class CoachDashboardReportDto
{
    // Trainees Stats
    public int TotalTrainees { get; set; }
    public int ActiveTrainees { get; set; }
    public int NewTraineesThisMonth { get; set; }

    // Programs Stats
    public int ActiveWorkoutPrograms { get; set; }
    public int ActiveDietPlans { get; set; }

    // Sessions Stats
    public int TotalSessionsThisMonth { get; set; }
    public double TotalVolumeThisMonth { get; set; }

    // Top Trainees
    public List<TopTraineeDto> TopTraineesByProgress { get; set; } = new();
    public List<TopTraineeDto> TopTraineesBySessions { get; set; } = new();
}

public class TopTraineeDto
{
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public int SessionsCount { get; set; }
    public double? WeightChange { get; set; }
    public double? BodyFatChange { get; set; }
}

// Coach Trainees Report
public class CoachTraineesReportDto
{
    public int TotalTrainees { get; set; }
    public int WithActiveSubscription { get; set; }
    public int WithoutSubscription { get; set; }
    public List<TraineeDetailDto> Trainees { get; set; } = new();
}

public class TraineeDetailDto
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime AssignedAt { get; set; }

    // Subscription
    public bool HasActiveSubscription { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }

    // Programs
    public int ActiveWorkoutPrograms { get; set; }
    public int ActiveDietPlans { get; set; }

    // Progress
    public int TotalSessions { get; set; }
    public int SessionsThisMonth { get; set; }
    public DateTime? LastSessionDate { get; set; }

    // Body Measurements
    public double? CurrentWeight { get; set; }
    public double? WeightChange { get; set; }
    public double? BodyFatPercent { get; set; }
    public DateTime? LastMeasurementDate { get; set; }
}

// Trainee Progress Report
public class TraineeProgressReportDto
{
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public DateTime AssignedAt { get; set; }

    // Body Progress
    public List<BodyProgressDto> BodyMeasurements { get; set; } = new();
    public double? StartWeight { get; set; }
    public double? CurrentWeight { get; set; }
    public double? TotalWeightChange { get; set; }
    public double? StartBodyFat { get; set; }
    public double? CurrentBodyFat { get; set; }
    public double? TotalBodyFatChange { get; set; }
    public double? StartMuscleMass { get; set; }
    public double? CurrentMuscleMass { get; set; }
    public double? TotalMuscleMassChange { get; set; }

    // Workout Progress
    public int TotalSessions { get; set; }
    public double TotalVolumeLifted { get; set; }
    public List<MonthlySessionsDto> MonthlySessions { get; set; } = new();

    // Active Programs
    public List<ActiveProgramDto> WorkoutPrograms { get; set; } = new();
    public List<ActivePlanDto> DietPlans { get; set; } = new();

    // Personal Records
    public List<PersonalRecordDto> PersonalRecords { get; set; } = new();
}

public class BodyProgressDto
{
    public DateTime DateRecorded { get; set; }
    public double WeightKg { get; set; }
    public double? BodyFatPercent { get; set; }
    public double? MuscleMass { get; set; }
    public double? Bmr { get; set; }
}

public class MonthlySessionsDto
{
    public string Month { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public double TotalVolume { get; set; }
}

public class ActiveProgramDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int RoutinesCount { get; set; }
}

public class ActivePlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public double TargetCalories { get; set; }
}

public class PersonalRecordDto
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public double MaxWeight { get; set; }
    public int Reps { get; set; }
    public DateTime AchievedAt { get; set; }
}
