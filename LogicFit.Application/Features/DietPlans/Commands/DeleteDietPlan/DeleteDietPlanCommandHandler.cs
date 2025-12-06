using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.DeleteDietPlan;

public class DeleteDietPlanCommandHandler : IRequestHandler<DeleteDietPlanCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteDietPlanCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(DeleteDietPlanCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var plan = await _context.DietPlans
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.TenantId == tenantId, cancellationToken);

        if (plan == null)
            throw new NotFoundException("DietPlan", request.Id);

        _context.DietPlans.Remove(plan);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
