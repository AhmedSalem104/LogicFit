using LogicFit.Domain.Common;
namespace LogicFit.Domain.Entities;
public sealed class JobExecutionLog : BaseEntity { public string JobName { get; set; } = string.Empty; public string Status { get; set; } = "Running"; public DateTime StartedAtUtc { get; set; } public DateTime? CompletedAtUtc { get; set; } public int AttemptCount { get; set; } public string? Error { get; set; } public string? Metadata { get; set; } }
