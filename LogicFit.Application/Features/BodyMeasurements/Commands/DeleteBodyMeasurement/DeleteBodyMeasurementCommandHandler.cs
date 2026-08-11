using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.BodyMeasurements.Commands.DeleteBodyMeasurement;

public class DeleteBodyMeasurementCommandHandler : IRequestHandler<DeleteBodyMeasurementCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteBodyMeasurementCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(DeleteBodyMeasurementCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            throw new Domain.Exceptions.ForbiddenException("An authenticated workspace user is required.");

        var measurement = await _context.BodyMeasurements
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.TenantId == tenantId, cancellationToken);

        if (measurement == null)
            throw new NotFoundException("BodyMeasurement", request.Id);

        var currentUser = await _context.Users
            .Where(u => u.Id == currentUserId && u.TenantId == tenantId && u.IsActive)
            .Select(u => (UserRole?)u.Role)
            .FirstOrDefaultAsync(cancellationToken);
        if (!currentUser.HasValue)
            throw new Domain.Exceptions.ForbiddenException("The authenticated user is not active in this workspace.");

        if (currentUser is UserRole.Coach or UserRole.Trainer or UserRole.FreelanceCoach)
        {
            var assigned = await _context.CoachClients.AnyAsync(cc => cc.TenantId == tenantId
                && cc.CoachId == currentUserId
                && cc.ClientId == measurement.ClientId
                && cc.IsActive
                && cc.UnassignedAt == null, cancellationToken);
            if (!assigned)
                throw new Domain.Exceptions.ForbiddenException("The client is not actively assigned to the current coach.");
        }
        else if (currentUser is not (UserRole.Owner or UserRole.Manager or UserRole.FreelanceOwner))
        {
            throw new Domain.Exceptions.ForbiddenException("You cannot delete this measurement.");
        }

        _context.BodyMeasurements.Remove(measurement);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
