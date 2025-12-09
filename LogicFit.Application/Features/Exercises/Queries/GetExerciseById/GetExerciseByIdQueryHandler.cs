using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Exercises.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Exercises.Queries.GetExerciseById;

public class GetExerciseByIdQueryHandler : IRequestHandler<GetExerciseByIdQuery, ExerciseDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetExerciseByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<ExerciseDto?> Handle(GetExerciseByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        return await _context.Exercises
            .Include(e => e.TargetMuscle)
            .Include(e => e.SecondaryMuscles)
                .ThenInclude(sm => sm.Muscle)
            .Where(e => e.Id == request.Id && (e.TenantId == null || e.TenantId == tenantId))
            .Select(e => new ExerciseDto
            {
                Id = e.Id,
                TenantId = e.TenantId,
                Name = e.Name,
                TargetMuscleId = e.TargetMuscleId,
                TargetMuscleName = e.TargetMuscle.Name,
                TargetMuscleBodyPart = e.TargetMuscle.BodyPart,
                PrimaryMuscleContributionPercent = 100 - e.SecondaryMuscles.Sum(sm => sm.ContributionPercent),
                SecondaryMuscles = e.SecondaryMuscles.Select(sm => new SecondaryMuscleDto
                {
                    MuscleId = sm.MuscleId,
                    MuscleName = sm.Muscle.Name,
                    BodyPart = sm.Muscle.BodyPart,
                    ContributionPercent = sm.ContributionPercent
                }).ToList(),
                ImageUrl = e.ImageUrl,
                VideoUrl = e.VideoUrl,
                Equipment = e.Equipment,
                IsHighImpact = e.IsHighImpact
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
