namespace LogicFit.Application.Features.Reports.DTOs;

/// <summary>
/// Read-only dashboard follow-up data. It deliberately contains only the
/// contact and subscription fields needed for an operator to decide what to
/// do next; it never exposes medical, training, or payment transaction data.
/// </summary>
public sealed class FollowUpAlertsDto
{
    public DateTime GeneratedAtUtc { get; set; }
    public List<FollowUpAlertItemDto> Alerts { get; set; } = new();
}

public sealed class FollowUpAlertItemDto
{
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? EndDate { get; set; }
    public decimal AmountRemaining { get; set; }
    public int? DaysSinceLastVisit { get; set; }
    public int Priority { get; set; }
}
