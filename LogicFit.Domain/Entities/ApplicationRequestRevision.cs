using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>Immutable snapshot written for every application submission or resubmission.</summary>
public class ApplicationRequestRevision : BaseEntity
{
    public Guid ApplicationRequestId { get; set; }
    public int RevisionNumber { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTime SubmittedAt { get; set; }
    public string? SubmittedBy { get; set; }
    public ApplicationRequest ApplicationRequest { get; set; } = null!;
}
