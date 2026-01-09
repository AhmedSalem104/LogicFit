using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Muscles.Commands.DeleteMuscle;

public class DeleteMuscleCommandHandler : IRequestHandler<DeleteMuscleCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteMuscleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(DeleteMuscleCommand request, CancellationToken cancellationToken)
    {
        var muscle = await _context.Muscles
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (muscle == null)
            throw new NotFoundException("Muscle", request.Id);

        // Check if muscle is used by any exercises
        var isUsedByExercises = await _context.Exercises
            .AnyAsync(e => e.TargetMuscleId == request.Id, cancellationToken);

        if (isUsedByExercises)
            throw new InvalidOperationException("Cannot delete muscle that is used by exercises");

        // Soft delete
        muscle.IsDeleted = true;
        muscle.DeletedAt = DateTime.UtcNow;
        muscle.DeletedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
