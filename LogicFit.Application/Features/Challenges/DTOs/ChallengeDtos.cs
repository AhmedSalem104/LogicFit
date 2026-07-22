using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Challenges.DTOs;

public class ChallengeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? TargetMetric { get; set; }
    public double? TargetValue { get; set; }
    public ChallengeStatus Status { get; set; }
    public string? CreatedByCoachName { get; set; }
    public int ParticipantCount { get; set; }
    public int CompletedCount { get; set; }
}

public class ChallengeLeaderboardEntryDto
{
    public int Rank { get; set; }
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public double CurrentProgress { get; set; }
    public double ProgressPercentage { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ClientChallengeDto
{
    public Guid Id { get; set; }
    public Guid ChallengeId { get; set; }
    public string ChallengeTitle { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string? ClientName { get; set; }
    public double CurrentProgress { get; set; }
    public double? TargetValue { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double ProgressPercentage { get; set; }
}
