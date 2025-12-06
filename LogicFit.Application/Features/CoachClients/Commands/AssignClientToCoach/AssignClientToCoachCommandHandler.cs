using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.CoachClients.Commands.AssignClientToCoach;

public class AssignClientToCoachCommandHandler : IRequestHandler<AssignClientToCoachCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public AssignClientToCoachCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Guid> Handle(AssignClientToCoachCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var currentUserId = Guid.Parse(_currentUserService.UserId!);

        // Get current user to check role
        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken)
            ?? throw new NotFoundException("Current user not found");

        // Determine coach ID
        Guid coachId;
        if (request.CoachId.HasValue)
        {
            // Only Owner can assign to other coaches
            if (currentUser.Role != UserRole.Owner)
                throw new ForbiddenException("Only Owner can assign clients to other coaches");

            coachId = request.CoachId.Value;
        }
        else
        {
            // Coach assigns to self
            if (currentUser.Role != UserRole.Coach && currentUser.Role != UserRole.Owner)
                throw new ForbiddenException("Only Coach or Owner can assign clients");

            coachId = currentUserId;
        }

        // Validate coach exists and is a coach
        var coach = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == coachId && (u.Role == UserRole.Coach || u.Role == UserRole.Owner), cancellationToken)
            ?? throw new NotFoundException("Coach not found");

        // Validate client exists and is a client
        var client = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.ClientId && u.Role == UserRole.Client, cancellationToken)
            ?? throw new NotFoundException("Client not found");

        // Check if client is already assigned to an active coach
        var existingAssignment = await _context.CoachClients
            .FirstOrDefaultAsync(cc => cc.ClientId == request.ClientId && cc.IsActive, cancellationToken);

        if (existingAssignment != null)
        {
            if (existingAssignment.CoachId == coachId)
                throw new ConflictException("Client is already assigned to this coach");

            throw new ConflictException("Client is already assigned to another coach. Unassign first.");
        }

        // Create new assignment
        var coachClient = new CoachClient
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CoachId = coachId,
            ClientId = request.ClientId,
            AssignedAt = _dateTimeService.UtcNow,
            IsActive = true,
            Notes = request.Notes
        };

        _context.CoachClients.Add(coachClient);
        await _context.SaveChangesAsync(cancellationToken);

        return coachClient.Id;
    }
}
