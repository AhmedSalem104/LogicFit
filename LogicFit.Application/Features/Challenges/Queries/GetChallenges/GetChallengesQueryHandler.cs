using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Challenges.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Challenges.Queries.GetChallenges;

public class GetChallengesQueryHandler : IRequestHandler<GetChallengesQuery, List<ChallengeDto>>
{
    private readonly IApplicationDbContext _context;

    public GetChallengesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChallengeDto>> Handle(GetChallengesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Challenges.AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        var challenges = await query
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
            .ToListAsync(cancellationToken);

        return challenges;
    }
}
