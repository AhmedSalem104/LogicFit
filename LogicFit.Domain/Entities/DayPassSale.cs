using LogicFit.Domain.Common;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class DayPassSale : TenantAuditableEntity
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string VisitorName { get; set; } = "زائر";
    public string? VisitorPhone { get; set; }
    public string? VisitorPhoneNormalized { get; set; }
    public Guid DayPassTypeId { get; set; }
    public string PassTypeCodeSnapshot { get; set; } = string.Empty;
    public string PassTypeNameSnapshot { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime VisitDate { get; set; }
    public string? Notes { get; set; }
    public DayPassStatus Status { get; set; } = DayPassStatus.Completed;
    public DateTime? WhatsappOpenedAt { get; set; }

    public virtual DayPassType DayPassType { get; set; } = null!;
    public virtual Payment? Payment { get; set; }
}
