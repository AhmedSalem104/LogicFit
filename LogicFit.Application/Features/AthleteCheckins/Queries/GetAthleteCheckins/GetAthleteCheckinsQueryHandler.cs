using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.AthleteCheckins.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.AthleteCheckins.Queries.GetAthleteCheckins;

public sealed class GetAthleteCheckinsQueryHandler : IRequestHandler<GetAthleteCheckinsQuery, List<AthleteCheckinDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUser;

    public GetAthleteCheckinsQueryHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUser)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUser = currentUser;
    }

    public async Task<List<AthleteCheckinDto>> Handle(GetAthleteCheckinsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated workspace user is required.");
        var role = await _context.Users.Where(x => x.Id == currentUserId && x.TenantId == tenantId && x.IsActive)
            .Select(x => (UserRole?)x.Role).FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("The authenticated user is not active in this workspace.");

        var query = _context.AthleteCheckins.Include(x => x.Client).ThenInclude(x => x.Profile)
            .Where(x => x.TenantId == tenantId && x.ClientId == request.ClientId);
        if (role == UserRole.Client && currentUserId != request.ClientId)
            throw new ForbiddenException("Clients can only view their own check-ins.");
        if (role is UserRole.Coach or UserRole.Trainer or UserRole.FreelanceCoach)
        {
            var assigned = await _context.CoachClients.AnyAsync(x => x.TenantId == tenantId && x.CoachId == currentUserId && x.ClientId == request.ClientId && x.IsActive && x.UnassignedAt == null, cancellationToken);
            if (!assigned) throw new ForbiddenException("The client is not actively assigned to the current coach.");
        }
        else if (role is not (UserRole.Client or UserRole.Owner or UserRole.Manager or UserRole.FreelanceOwner))
            throw new ForbiddenException("You cannot view coaching check-ins.");

        if (request.FromDate.HasValue) query = query.Where(x => x.CheckinDate >= request.FromDate.Value.Date);
        if (request.ToDate.HasValue) query = query.Where(x => x.CheckinDate <= request.ToDate.Value.Date);
        var rows = await query.OrderByDescending(x => x.CheckinDate).ToListAsync(cancellationToken);
        return rows.Select(x => new AthleteCheckinDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            ClientId = x.ClientId,
            ClientName = x.Client.Profile != null ? x.Client.Profile.FullName : x.Client.Email,
            CheckinDate = x.CheckinDate,
            SleepHours = x.SleepHours,
            SleepQuality = x.SleepQuality,
            Fatigue = x.Fatigue,
            Soreness = x.Soreness,
            Stress = x.Stress,
            Mood = x.Mood,
            RestingHeartRate = x.RestingHeartRate,
            Hrv = x.Hrv,
            BodyweightKg = x.BodyweightKg,
            Notes = x.Notes
        }).ToList();
    }
}
