using LogicFit.Application.Features.Commissions.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Commissions.Queries.GetCommissionRules;

public class GetCommissionRulesQuery : IRequest<List<CommissionRuleDto>>
{
    public bool? IsActive { get; set; }
}
