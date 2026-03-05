using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Challenges.Commands.UpdateChallenge;

public class UpdateChallengeCommandHandler : IRequestHandler<UpdateChallengeCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateChallengeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateChallengeCommand request, CancellationToken cancellationToken)
    {
        var challenge = await _context.Challenges
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (challenge == null)
            throw new NotFoundException("Challenge", request.Id);

        if (request.Title != null)
            challenge.Title = request.Title;

        if (request.Description != null)
            challenge.Description = request.Description;

        if (request.EndDate.HasValue)
            challenge.EndDate = request.EndDate.Value;

        if (request.Status.HasValue)
            challenge.Status = request.Status.Value;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
