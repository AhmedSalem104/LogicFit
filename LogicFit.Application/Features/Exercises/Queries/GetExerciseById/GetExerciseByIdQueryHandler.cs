using System.Text.Json;
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

        var exercise = await _context.Exercises
            .Include(e => e.TargetMuscle)
            .Include(e => e.SecondaryMuscles)
                .ThenInclude(sm => sm.Muscle)
            .Where(e => e.Id == request.Id && (e.TenantId == null || e.TenantId == tenantId))
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
            .FirstOrDefaultAsync(cancellationToken);

        if (exercise == null) return null;

        return new ExerciseDto
        {
            Id = exercise.Id,
            TenantId = exercise.TenantId,
            Name = exercise.Name,
            NameAr = exercise.NameAr,
            Description = exercise.Description,
            DescriptionAr = exercise.DescriptionAr,
            TargetMuscleId = exercise.TargetMuscleId,
            TargetMuscleName = exercise.TargetMuscleName,
            TargetMuscleBodyPart = exercise.TargetMuscleBodyPart,
            PrimaryMuscleContributionPercent = 100 - exercise.SecondaryMusclesSum,
            SecondaryMuscles = exercise.SecondaryMuscles,
            ImageUrl = exercise.ImageUrl,
            VideoUrl = exercise.VideoUrl,
            Icon = exercise.Icon,
            Equipment = exercise.Equipment,
            IsHighImpact = exercise.IsHighImpact,
            Difficulty = exercise.Difficulty,
            Category = exercise.Category,
            MovementPattern = exercise.MovementPattern,
            Mechanic = exercise.Mechanic,
            Force = exercise.Force,
            Instructions = DeserializeJsonArray(exercise.Instructions),
            InstructionsAr = DeserializeJsonArray(exercise.InstructionsAr),
            Tips = DeserializeJsonArray(exercise.Tips),
            TipsAr = DeserializeJsonArray(exercise.TipsAr),
            CommonMistakes = DeserializeJsonArray(exercise.CommonMistakes),
            CommonMistakesAr = DeserializeJsonArray(exercise.CommonMistakesAr),
            RepsRange = exercise.RepsRange,
            SetsRange = exercise.SetsRange,
            RestSeconds = exercise.RestSeconds,
            Tempo = exercise.Tempo
        };
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
