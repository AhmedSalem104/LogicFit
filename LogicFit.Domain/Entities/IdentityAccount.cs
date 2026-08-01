using LogicFit.Domain.Common;

namespace LogicFit.Domain.Entities;

/// <summary>
/// Global login identity. Workspace-specific access remains represented by a User and a
/// WorkspaceMembership so existing tenant-owned domain relationships stay intact.
/// </summary>
public class IdentityAccount : AuditableEntity
{
    /// <summary>Global display name collected during identity registration, independent of any workspace profile.</summary>
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    /// <summary>
    /// A global identity cannot sign in until ownership of its email address has been proved by a
    /// one-time verification link. Existing identities are backfilled during the migration.
    /// </summary>
    public DateTime? EmailVerifiedAt { get; set; }
    public string? PhoneNumber { get; set; }
    public string? NormalizedPhoneNumber { get; set; }
    public DateTime? PhoneVerifiedAt { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<WorkspaceMembership> Memberships { get; set; } = new List<WorkspaceMembership>();
    public ICollection<ApplicationRequest> Applications { get; set; } = new List<ApplicationRequest>();
    public ICollection<IdentityEmailActionToken> EmailActionTokens { get; set; } = new List<IdentityEmailActionToken>();
    public ICollection<OtpChallenge> OtpChallenges { get; set; } = new List<OtpChallenge>();
}
