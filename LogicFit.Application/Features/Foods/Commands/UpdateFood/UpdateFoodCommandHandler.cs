using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Foods.Commands.UpdateFood;

public class UpdateFoodCommandHandler : IRequestHandler<UpdateFoodCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateFoodCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateFoodCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var food = await _context.Foods
            .FirstOrDefaultAsync(f => f.Id == request.Id && f.TenantId == tenantId, cancellationToken);

        if (food == null)
            throw new NotFoundException("Food", request.Id);

        food.Name = request.Name;
        food.Category = request.Category;
        food.CaloriesPer100g = request.CaloriesPer100g;
        food.ProteinPer100g = request.ProteinPer100g;
        food.CarbsPer100g = request.CarbsPer100g;
        food.FatsPer100g = request.FatsPer100g;
        food.FiberPer100g = request.FiberPer100g;
        food.AlternativeGroupId = request.AlternativeGroupId;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
