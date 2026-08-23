using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// Feedback submitted by a tenant client about the gym experience.
/// </summary>
public class ClientFeedback : TenantAuditableEntity
{
    public Guid ClientId { get; set; }
    public int Rating { get; set; }
    public string NoteType { get; set; } = "general";
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }

    public virtual User Client { get; set; } = null!;
    public virtual User? ReviewedBy { get; set; }
}
