using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// Global login identity. Workspace-specific access remains represented by a User and a
/// WorkspaceMembership so existing tenant-owned domain relationships stay intact.
/// </summary>
public class IdentityAccount : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? NormalizedPhoneNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public ICollection<WorkspaceMembership> Memberships { get; set; } = new List<WorkspaceMembership>();
    public ICollection<ApplicationRequest> Applications { get; set; } = new List<ApplicationRequest>();
}
