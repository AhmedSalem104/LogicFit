using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.CoachClients.Commands.UpdateCoachClient;

public class UpdateCoachClientCommandHandler : IRequestHandler<UpdateCoachClientCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateCoachClientCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateCoachClientCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var coachClient = await _context.CoachClients
            .FirstOrDefaultAsync(cc => cc.Id == request.Id && cc.TenantId == tenantId, cancellationToken);

        if (coachClient == null)
            return false;

        // Update coach if new coach is provided
        if (request.NewCoachId.HasValue)
        {
            // Verify new coach exists and is a valid coach
            var newCoach = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.NewCoachId.Value &&
                                         u.TenantId == tenantId &&
                                         (u.Role == UserRole.Coach || u.Role == UserRole.Owner),
                                         cancellationToken);

            if (newCoach == null)
                return false;

            coachClient.CoachId = request.NewCoachId.Value;
        }

        // Update active status if provided
        if (request.IsActive.HasValue)
        {
            coachClient.IsActive = request.IsActive.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
