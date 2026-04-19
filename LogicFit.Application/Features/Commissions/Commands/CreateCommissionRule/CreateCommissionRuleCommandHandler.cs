using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;

namespace LogicFit.Application.Features.Commissions.Commands.CreateCommissionRule;

public class CreateCommissionRuleCommandHandler : IRequestHandler<CreateCommissionRuleCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateCommissionRuleCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateCommissionRuleCommand request, CancellationToken cancellationToken)
    {
        if (request.EmployeeId == null && request.Role == null)
            throw new DomainException("Either EmployeeId or Role must be specified");

        var rule = new CommissionRule
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.GetCurrentTenantId(),
            EmployeeId = request.EmployeeId,
            Role = request.Role,
            SourceType = request.SourceType,
            Type = request.Type,
            Value = request.Value,
            MinAmount = request.MinAmount,
            IsActive = request.IsActive
        };
        _context.CommissionRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);
        return rule.Id;
    }
}
