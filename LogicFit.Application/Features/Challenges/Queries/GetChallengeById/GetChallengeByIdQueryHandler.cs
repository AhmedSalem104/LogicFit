using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Challenges.DTOs;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Challenges.Queries.GetChallengeById;

public class GetChallengeByIdQueryHandler : IRequestHandler<GetChallengeByIdQuery, ChallengeDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetChallengeByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<ChallengeDto> Handle(GetChallengeByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var challenge = await _context.Challenges
            .Where(c => c.Id == request.Id && c.TenantId == tenantId && !c.IsDeleted)
            .Select(c => new ChallengeDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                TargetMetric = c.TargetMetric,
                TargetValue = c.TargetValue,
                Status = c.Status,
                CreatedByCoachName = c.CreatedByCoach.Profile != null ? c.CreatedByCoach.Profile.FullName ?? c.CreatedByCoach.Email : c.CreatedByCoach.Email,
                ParticipantCount = c.Participants.Count(),
                CompletedCount = c.Participants.Count(p => p.IsCompleted)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge == null)
            throw new NotFoundException("Challenge", request.Id);

        return challenge;
    }
}
