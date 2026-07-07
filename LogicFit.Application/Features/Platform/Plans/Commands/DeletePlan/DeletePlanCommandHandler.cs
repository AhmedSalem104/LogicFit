using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Plans.Commands.DeletePlan;

public class DeletePlanCommandHandler : IRequestHandler<DeletePlanCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public DeletePlanCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeletePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan == null)
        {
            throw new NotFoundException(nameof(Plan), request.Id);
        }

        var inUse = await _context.TenantSubscriptions
            .AnyAsync(s => s.PlanId == request.Id &&
                           (s.Status == Domain.Enums.TenantSubscriptionStatus.Active ||
                            s.Status == Domain.Enums.TenantSubscriptionStatus.Trial), cancellationToken);
        if (inUse)
        {
            throw new ConflictException("Cannot delete a plan that has active or trial subscriptions. Deactivate it instead.");
        }

        // Soft delete (SaveChanges converts the delete of an ISoftDeletable to IsDeleted = true).
        _context.Plans.Remove(plan);
        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
