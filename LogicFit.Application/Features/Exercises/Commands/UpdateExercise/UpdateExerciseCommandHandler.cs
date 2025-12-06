using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Exercises.Commands.UpdateExercise;

public class UpdateExerciseCommandHandler : IRequestHandler<UpdateExerciseCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IFileUploadService _fileUploadService;

    public UpdateExerciseCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IFileUploadService fileUploadService)
    {
        _context = context;
        _tenantService = tenantService;
        _fileUploadService = fileUploadService;
    }

    public async Task<bool> Handle(UpdateExerciseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var exercise = await _context.Exercises
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == tenantId, cancellationToken);

        if (exercise == null)
            throw new NotFoundException("Exercise", request.Id);

        // Upload new image if provided
        if (request.Image != null)
        {
            // Delete old image if exists
            if (!string.IsNullOrEmpty(exercise.ImageUrl))
            {
                await _fileUploadService.DeleteFileAsync(exercise.ImageUrl);
            }
            exercise.ImageUrl = await _fileUploadService.UploadImageAsync(request.Image, "exercises");
        }

        // Upload new video if provided
        if (request.Video != null)
        {
            // Delete old video if exists
            if (!string.IsNullOrEmpty(exercise.VideoUrl))
            {
                await _fileUploadService.DeleteFileAsync(exercise.VideoUrl);
            }
            exercise.VideoUrl = await _fileUploadService.UploadVideoAsync(request.Video, "exercises");
        }

        exercise.Name = request.Name;
        exercise.TargetMuscleId = request.TargetMuscleId;
        exercise.Equipment = request.Equipment;
        exercise.IsHighImpact = request.IsHighImpact;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
