using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Coaches.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Coaches.Queries.GetCoaches;

public class GetCoachesQueryHandler : IRequestHandler<GetCoachesQuery, List<CoachDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetCoachesQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<CoachDto>> Handle(GetCoachesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Users
            .Include(u => u.Profile)
            .Where(u => u.TenantId == tenantId && u.Role == UserRole.Coach)
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query = query.Where(u =>
                (u.PhoneNumber != null && u.PhoneNumber.Contains(request.SearchTerm)) ||
                (u.Email != null && u.Email.Contains(request.SearchTerm)) ||
                (u.Profile != null && u.Profile.FullName != null && u.Profile.FullName.Contains(request.SearchTerm)));

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new CoachDto
            {
                Id = u.Id,
                TenantId = u.TenantId,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                Profile = u.Profile != null ? new CoachProfileDto
                {
                    FullName = u.Profile.FullName,
                    ProfilePictureUrl = u.Profile.ProfilePictureUrl,
                    Gender = (int?)u.Profile.Gender,
                    BirthDate = u.Profile.BirthDate
                } : null,
                TraineeCount = _context.CoachClients.Count(cc => cc.CoachId == u.Id && cc.IsActive && !cc.IsDeleted)
            })
            .ToListAsync(cancellationToken);
    }
}
