using LogicFit.Application.Features.Commissions.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Commissions.Queries.GetCommissions;

public class GetCommissionsQuery : IRequest<List<CommissionDto>>
{
    public Guid? EmployeeId { get; set; }
    public CommissionStatus? Status { get; set; }
    public CommissionSourceType? SourceType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
