using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using MediatR;

namespace LogicFit.Application.Features.Exercises.Commands.CreateExercise;

public class CreateExerciseCommandHandler : IRequestHandler<CreateExerciseCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IFileUploadService _fileUploadService;

    public CreateExerciseCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IFileUploadService fileUploadService)
    {
        _context = context;
        _tenantService = tenantService;
        _fileUploadService = fileUploadService;
    }

    public async Task<int> Handle(CreateExerciseCommand request, CancellationToken cancellationToken)
    {
        string? imageUrl = null;
        string? videoUrl = null;

        // Upload image if provided
        if (request.Image != null)
        {
            imageUrl = await _fileUploadService.UploadImageAsync(request.Image, "exercises");
        }

        // Upload video if provided
        if (request.Video != null)
        {
            videoUrl = await _fileUploadService.UploadVideoAsync(request.Video, "exercises");
        }

        var exercise = new Exercise
        {
            TenantId = _tenantService.GetCurrentTenantId(),
            Name = request.Name,
            TargetMuscleId = request.TargetMuscleId,
            ImageUrl = imageUrl,
            VideoUrl = videoUrl,
            Equipment = request.Equipment,
            IsHighImpact = request.IsHighImpact
        };

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync(cancellationToken);

        return exercise.Id;
    }
}
