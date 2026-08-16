using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.AthleteCheckins.Commands.DeleteAthleteCheckin;

public sealed class DeleteAthleteCheckinCommandHandler : IRequestHandler<DeleteAthleteCheckinCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUser;

    public DeleteAthleteCheckinCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUser)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(DeleteAthleteCheckinCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated workspace user is required.");
        var checkin = await _context.AthleteCheckins.FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == tenantId && x.ClientId == request.ClientId, cancellationToken)
            ?? throw new NotFoundException("AthleteCheckin", request.Id);
        var role = await _context.Users.Where(x => x.Id == currentUserId && x.TenantId == tenantId && x.IsActive)
            .Select(x => (UserRole?)x.Role).FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("The authenticated user is not active in this workspace.");
        if (role == UserRole.Client && currentUserId != checkin.ClientId)
            throw new ForbiddenException("Clients can only delete their own check-ins.");
        if (role is UserRole.Coach or UserRole.Trainer or UserRole.FreelanceCoach)
        {
            if (!await _context.CoachClients.AnyAsync(x => x.TenantId == tenantId && x.CoachId == currentUserId && x.ClientId == checkin.ClientId && x.IsActive && x.UnassignedAt == null, cancellationToken))
                throw new ForbiddenException("The client is not actively assigned to the current coach.");
        }
        else if (role is not (UserRole.Client or UserRole.Owner or UserRole.Manager or UserRole.FreelanceOwner))
            throw new ForbiddenException("You cannot manage coaching check-ins.");
        _context.AthleteCheckins.Remove(checkin);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
