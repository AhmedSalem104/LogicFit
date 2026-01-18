using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.CoachClients.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.CoachClients.Queries.GetCoachClientById;

public class GetCoachClientByIdQueryHandler : IRequestHandler<GetCoachClientByIdQuery, CoachClientDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetCoachClientByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<CoachClientDto?> Handle(GetCoachClientByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var coachClient = await _context.CoachClients
            .Include(cc => cc.Coach)
                .ThenInclude(c => c.Profile)
            .Include(cc => cc.Client)
                .ThenInclude(c => c.Profile)
            .FirstOrDefaultAsync(cc => cc.Id == request.Id && cc.TenantId == tenantId, cancellationToken);

        if (coachClient == null)
            return null;

        return new CoachClientDto
        {
            Id = coachClient.Id,
            CoachId = coachClient.CoachId,
            CoachName = coachClient.Coach?.Profile?.FullName ?? coachClient.Coach?.Email,
            ClientId = coachClient.ClientId,
            ClientName = coachClient.Client?.Profile?.FullName ?? coachClient.Client?.Email,
            ClientPhone = coachClient.Client?.PhoneNumber,
            AssignedAt = coachClient.AssignedAt,
            IsActive = coachClient.IsActive
        };
    }
}
