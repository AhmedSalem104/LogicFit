using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class GateAccessLog : TenantAuditableEntity
{
    public Guid? ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? MembershipCardId { get; set; }
    public DateTime AccessTime { get; set; }
    public GateAccessResult Result { get; set; }
    public GateAccessMethod Method { get; set; }
    public GateDenyReason DenyReason { get; set; }
    public string? Notes { get; set; }
    public string? ScannedCode { get; set; }

    public virtual User? Client { get; set; }
    public virtual Branch? Branch { get; set; }
    public virtual MembershipCard? MembershipCard { get; set; }
}
