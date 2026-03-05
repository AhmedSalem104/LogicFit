using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using MediatR;

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
            CreatedByCoachId = Guid.Parse(_currentUserService.UserId!)
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync(cancellationToken);

        if (request.ClientIds?.Any() == true)
        {
            var clientChallenges = request.ClientIds.Select(clientId => new ClientChallenge
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
