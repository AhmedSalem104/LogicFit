using LogicFit.Domain.Common;
namespace LogicFit.Domain.Entities;
public sealed class OutboxMessage : BaseEntity { public string Type { get; set; } = string.Empty; public string Payload { get; set; } = string.Empty; public DateTime OccurredAtUtc { get; set; } public DateTime? ProcessedAtUtc { get; set; } public DateTime? FailedAtUtc { get; set; } public int AttemptCount { get; set; } public string? LastError { get; set; } public string IdempotencyKey { get; set; } = string.Empty; }
