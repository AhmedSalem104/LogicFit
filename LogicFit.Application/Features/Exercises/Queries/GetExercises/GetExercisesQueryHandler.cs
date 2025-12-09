using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Exercises.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Exercises.Queries.GetExercises;

public class GetExercisesQueryHandler : IRequestHandler<GetExercisesQuery, List<ExerciseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetExercisesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ExerciseDto>> Handle(GetExercisesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Exercises
            .Include(e => e.TargetMuscle)
            .Include(e => e.SecondaryMuscles)
                .ThenInclude(sm => sm.Muscle)
            .Where(e => e.TenantId == null || e.TenantId == tenantId)
            .AsQueryable();

        if (request.TargetMuscleId.HasValue)
            query = query.Where(e => e.TargetMuscleId == request.TargetMuscleId.Value);

        if (!string.IsNullOrEmpty(request.Equipment))
            query = query.Where(e => e.Equipment == request.Equipment);

        if (request.IsHighImpact.HasValue)
            query = query.Where(e => e.IsHighImpact == request.IsHighImpact.Value);

        return await query
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
            .ToListAsync(cancellationToken);
    }
}
