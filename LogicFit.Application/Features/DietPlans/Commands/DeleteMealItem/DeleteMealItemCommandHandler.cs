using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.DeleteMealItem;

public class DeleteMealItemCommandHandler : IRequestHandler<DeleteMealItemCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICoachPlanAccessService _accessService;

    public DeleteMealItemCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICoachPlanAccessService accessService)
    {
        _context = context;
        _tenantService = tenantService;
        _accessService = accessService;
    }

    public async Task<bool> Handle(DeleteMealItemCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var item = await _context.MealItems
            .FirstOrDefaultAsync(i => i.Id == request.Id && i.TenantId == tenantId, cancellationToken);

        if (item == null)
            throw new NotFoundException("MealItem", request.Id);

        await _accessService.EnsureCanManageMealAsync(item.MealId, cancellationToken);

        _context.MealItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
