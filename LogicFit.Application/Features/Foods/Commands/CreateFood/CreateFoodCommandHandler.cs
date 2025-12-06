using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using MediatR;

namespace LogicFit.Application.Features.Foods.Commands.CreateFood;

public class CreateFoodCommandHandler : IRequestHandler<CreateFoodCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateFoodCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<int> Handle(CreateFoodCommand request, CancellationToken cancellationToken)
    {
        var food = new Food
        {
            TenantId = _tenantService.GetCurrentTenantId(),
            Name = request.Name,
            Category = request.Category,
            CaloriesPer100g = request.CaloriesPer100g,
            ProteinPer100g = request.ProteinPer100g,
            CarbsPer100g = request.CarbsPer100g,
            FatsPer100g = request.FatsPer100g,
            FiberPer100g = request.FiberPer100g,
            AlternativeGroupId = request.AlternativeGroupId,
            IsVerified = false
        };

        _context.Foods.Add(food);
        await _context.SaveChangesAsync(cancellationToken);

        return food.Id;
    }
}
