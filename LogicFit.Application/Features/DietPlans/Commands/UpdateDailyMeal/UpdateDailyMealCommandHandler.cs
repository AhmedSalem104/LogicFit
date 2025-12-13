using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.UpdateDailyMeal;

public class UpdateDailyMealCommandHandler : IRequestHandler<UpdateDailyMealCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateDailyMealCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateDailyMealCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var meal = await _context.DailyMeals
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.TenantId == tenantId, cancellationToken);

        if (meal == null)
            throw new NotFoundException("DailyMeal", request.Id);

        meal.Name = request.Name;
        meal.OrderIndex = request.OrderIndex;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
