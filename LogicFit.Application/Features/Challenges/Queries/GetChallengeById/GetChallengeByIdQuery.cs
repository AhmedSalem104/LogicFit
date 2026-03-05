using LogicFit.Application.Features.Challenges.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Challenges.Queries.GetChallengeById;

public class GetChallengeByIdQuery : IRequest<ChallengeDto>
{
    public Guid Id { get; set; }
}
