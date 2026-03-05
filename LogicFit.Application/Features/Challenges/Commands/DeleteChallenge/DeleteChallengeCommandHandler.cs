using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Challenges.Commands.DeleteChallenge;

public class DeleteChallengeCommandHandler : IRequestHandler<DeleteChallengeCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteChallengeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(DeleteChallengeCommand request, CancellationToken cancellationToken)
    {
        var challenge = await _context.Challenges
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (challenge == null)
            throw new NotFoundException("Challenge", request.Id);

        challenge.IsDeleted = true;
        challenge.DeletedAt = DateTime.UtcNow;
        challenge.DeletedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
