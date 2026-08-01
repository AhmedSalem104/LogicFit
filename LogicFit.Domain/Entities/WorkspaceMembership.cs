using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class WorkspaceMembership : TenantAuditableEntity
{
    public Guid IdentityAccountId { get; set; }
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }
    public WorkspaceMembershipStatus Status { get; set; } = WorkspaceMembershipStatus.PendingPlatformApproval;
    public Guid? SponsoredByMembershipId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedBy { get; set; }
    public string? DecisionReason { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public IdentityAccount IdentityAccount { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;
    public WorkspaceMembership? SponsoredByMembership { get; set; }
}
