using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.ClientDashboard.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMyCoach;

public class GetMyCoachQueryHandler : IRequestHandler<GetMyCoachQuery, MyCoachDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetMyCoachQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<MyCoachDto?> Handle(GetMyCoachQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var userId = Guid.Parse(_currentUserService.UserId!);

        return await _context.CoachClients
            .Include(cc => cc.Coach).ThenInclude(c => c.Profile)
            .Where(cc => cc.TenantId == tenantId && cc.ClientId == userId && cc.IsActive)
            .Select(cc => new MyCoachDto
            {
                CoachId = cc.CoachId,
                FullName = cc.Coach.Profile != null ? cc.Coach.Profile.FullName : null,
                Email = cc.Coach.Email,
                PhoneNumber = cc.Coach.PhoneNumber,
                ProfilePictureUrl = cc.Coach.Profile != null ? cc.Coach.Profile.ProfilePictureUrl : null,
                AssignedAt = cc.AssignedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
