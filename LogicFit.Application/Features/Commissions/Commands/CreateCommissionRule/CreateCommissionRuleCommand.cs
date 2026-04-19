using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Commissions.Commands.CreateCommissionRule;

public class CreateCommissionRuleCommand : IRequest<Guid>
{
    public Guid? EmployeeId { get; set; }
    public UserRole? Role { get; set; }
    public CommissionSourceType SourceType { get; set; }
    public CommissionRuleType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinAmount { get; set; }
    public bool IsActive { get; set; } = true;
}
