using LogicFit.Application.Features.GateAccess.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.GateAccess.Queries.GetGateAccessLogs;

public class GetGateAccessLogsQuery : IRequest<List<GateAccessLogDto>>
{
    public Guid? ClientId { get; set; }
    public Guid? BranchId { get; set; }
    public GateAccessResult? Result { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Take { get; set; } = 200;
}
