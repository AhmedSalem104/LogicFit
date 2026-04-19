using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.GateAccess.DTOs;

public class GateAccessLogDto
{
    public Guid Id { get; set; }
    public Guid? ClientId { get; set; }
    public string? ClientName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateTime AccessTime { get; set; }
    public GateAccessResult Result { get; set; }
    public string ResultName => Result.ToString();
    public GateAccessMethod Method { get; set; }
    public string MethodName => Method.ToString();
    public GateDenyReason DenyReason { get; set; }
    public string DenyReasonName => DenyReason.ToString();
    public string? Notes { get; set; }
    public string? ScannedCode { get; set; }
}

public class GateCheckInResultDto
{
    public bool Granted { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? AttendanceId { get; set; }
    public Guid? ClientId { get; set; }
    public string? ClientName { get; set; }
    public Guid? BranchId { get; set; }
    public GateDenyReason DenyReason { get; set; }
}
