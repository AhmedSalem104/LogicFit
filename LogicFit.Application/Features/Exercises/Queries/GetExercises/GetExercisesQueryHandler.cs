using System.Text.Json;
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

        var exercises = await query
            .Select(e => new
            {
                e.Id,
                e.TenantId,
                e.Name,
                e.NameAr,
                e.Description,
                e.DescriptionAr,
                e.TargetMuscleId,
                TargetMuscleName = e.TargetMuscle.Name,
                TargetMuscleBodyPart = e.TargetMuscle.BodyPart,
                SecondaryMusclesSum = e.SecondaryMuscles.Sum(sm => sm.ContributionPercent),
                SecondaryMuscles = e.SecondaryMuscles.Select(sm => new SecondaryMuscleDto
                {
                    MuscleId = sm.MuscleId,
                    MuscleName = sm.Muscle.Name,
                    BodyPart = sm.Muscle.BodyPart,
                    ContributionPercent = sm.ContributionPercent
                }).ToList(),
                e.ImageUrl,
                e.VideoUrl,
                e.Icon,
                e.Equipment,
                e.IsHighImpact,
                e.Difficulty,
                e.Category,
                e.MovementPattern,
                e.Mechanic,
                e.Force,
                e.Instructions,
                e.InstructionsAr,
                e.Tips,
                e.TipsAr,
                e.CommonMistakes,
                e.CommonMistakesAr,
                e.RepsRange,
                e.SetsRange,
                e.RestSeconds,
                e.Tempo
            })
            .ToListAsync(cancellationToken);

        return exercises.Select(e => new ExerciseDto
        {
            Id = e.Id,
            TenantId = e.TenantId,
            Name = e.Name,
            NameAr = e.NameAr,
            Description = e.Description,
            DescriptionAr = e.DescriptionAr,
            TargetMuscleId = e.TargetMuscleId,
            TargetMuscleName = e.TargetMuscleName,
            TargetMuscleBodyPart = e.TargetMuscleBodyPart,
            PrimaryMuscleContributionPercent = 100 - e.SecondaryMusclesSum,
            SecondaryMuscles = e.SecondaryMuscles,
            ImageUrl = e.ImageUrl,
            VideoUrl = e.VideoUrl,
            Icon = e.Icon,
            Equipment = e.Equipment,
            IsHighImpact = e.IsHighImpact,
            Difficulty = e.Difficulty,
            Category = e.Category,
            MovementPattern = e.MovementPattern,
            Mechanic = e.Mechanic,
            Force = e.Force,
            Instructions = DeserializeJsonArray(e.Instructions),
            InstructionsAr = DeserializeJsonArray(e.InstructionsAr),
            Tips = DeserializeJsonArray(e.Tips),
            TipsAr = DeserializeJsonArray(e.TipsAr),
            CommonMistakes = DeserializeJsonArray(e.CommonMistakes),
            CommonMistakesAr = DeserializeJsonArray(e.CommonMistakesAr),
            RepsRange = e.RepsRange,
            SetsRange = e.SetsRange,
            RestSeconds = e.RestSeconds,
            Tempo = e.Tempo
        }).ToList();
    }

    private static List<string>? DeserializeJsonArray(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return null;
        }
    }
}
