using LogicFit.Application.Features.GateAccess.DTOs;
using MediatR;

namespace LogicFit.Application.Features.GateAccess.Commands.GateCheckInByQr;

public class GateCheckInByQrCommand : IRequest<GateCheckInResultDto>
{
    public string QrCode { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
}
