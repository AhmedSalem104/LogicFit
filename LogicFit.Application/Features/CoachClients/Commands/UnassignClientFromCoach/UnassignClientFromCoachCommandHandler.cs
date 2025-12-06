using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.CoachClients.Commands.UnassignClientFromCoach;

public class UnassignClientFromCoachCommandHandler : IRequestHandler<UnassignClientFromCoachCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public UnassignClientFromCoachCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Unit> Handle(UnassignClientFromCoachCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = Guid.Parse(_currentUserService.UserId!);

        // Get current user to check role
        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken)
            ?? throw new NotFoundException("Current user not found");

        // Find active assignment
        var assignment = await _context.CoachClients
            .FirstOrDefaultAsync(cc => cc.ClientId == request.ClientId && cc.IsActive, cancellationToken)
            ?? throw new NotFoundException("No active assignment found for this client");

        // Check authorization: Owner can unassign anyone, Coach can only unassign their own clients
        if (currentUser.Role == UserRole.Coach && assignment.CoachId != currentUserId)
            throw new ForbiddenException("You can only unassign your own clients");

        if (currentUser.Role == UserRole.Client)
            throw new ForbiddenException("Clients cannot unassign");

        // Unassign
        assignment.IsActive = false;
        assignment.UnassignedAt = _dateTimeService.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
