using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Challenges.Commands.CreateChallenge;

public class CreateChallengeCommandHandler : IRequestHandler<CreateChallengeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public CreateChallengeCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateChallengeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUserService.UserId, out var creatorId))
            throw new UnauthorizedException("An authenticated workspace user is required.");

        var creator = await _context.Users
            .Where(u => u.Id == creatorId && u.TenantId == tenantId && u.IsActive)
            .Select(u => new { u.Id, u.Role })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("The authenticated user is not active in this workspace.");

        if (creator.Role is not (UserRole.Owner or UserRole.Manager or UserRole.Coach
            or UserRole.Trainer or UserRole.FreelanceOwner or UserRole.FreelanceCoach))
            throw new ForbiddenException("The authenticated user cannot create challenges.");

        var clientIds = request.ClientIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? [];
        if (clientIds.Count > 0)
        {
            var clientsQuery = _context.Users.Where(u => clientIds.Contains(u.Id)
                && u.TenantId == tenantId
                && u.Role == UserRole.Client
                && u.IsActive);

            if (creator.Role is UserRole.Coach or UserRole.Trainer or UserRole.FreelanceCoach)
            {
                clientsQuery = clientsQuery.Where(u => _context.CoachClients.Any(cc => cc.TenantId == tenantId
                    && cc.CoachId == creator.Id
                    && cc.ClientId == u.Id
                    && cc.IsActive
                    && cc.UnassignedAt == null));
            }

            var validClientIds = await clientsQuery.Select(u => u.Id).ToListAsync(cancellationToken);
            if (validClientIds.Count != clientIds.Count)
                throw new NotFoundException("One or more selected clients are not available in this workspace.");
        }

        var challenge = new Challenge
        {
            TenantId = tenantId,
            Title = request.Title,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TargetMetric = request.TargetMetric,
            TargetValue = request.TargetValue,
            Status = ChallengeStatus.Active,
            CreatedByCoachId = creator.Id
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync(cancellationToken);

        if (clientIds.Count > 0)
        {
            var clientChallenges = clientIds.Select(clientId => new ClientChallenge
            {
                TenantId = tenantId,
                ChallengeId = challenge.Id,
                ClientId = clientId,
                CurrentProgress = 0,
                IsCompleted = false
            });

            _context.ClientChallenges.AddRange(clientChallenges);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return challenge.Id;
    }
}
