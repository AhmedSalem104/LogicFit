using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.DietPlans.Commands.CreateDietPlan;

public class CreateDietPlanCommandHandler : IRequestHandler<CreateDietPlanCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public CreateDietPlanCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateDietPlanCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = Guid.Parse(_currentUserService.UserId!);
        var currentUserRole = await _context.Users
            .Where(u => u.Id == currentUserId)
            .Select(u => u.Role)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentUserRole == UserRole.Client)
            throw new ForbiddenException("Clients cannot create diet plans");

        var plan = new DietPlan
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.GetCurrentTenantId(),
            CoachId = Guid.Parse(_currentUserService.UserId!),
            ClientId = request.ClientId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = PlanStatus.Active,
            TargetCalories = request.TargetCalories,
            TargetProtein = request.TargetProtein,
            TargetCarbs = request.TargetCarbs,
            TargetFats = request.TargetFats
        };

        _context.DietPlans.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }
}
