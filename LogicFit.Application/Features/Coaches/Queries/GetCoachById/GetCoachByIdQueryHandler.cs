using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Coaches.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Coaches.Queries.GetCoachById;

public class GetCoachByIdQueryHandler : IRequestHandler<GetCoachByIdQuery, CoachDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetCoachByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<CoachDto?> Handle(GetCoachByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        return await _context.Users
            .Include(u => u.Profile)
            .Where(u => u.Id == request.Id && u.TenantId == tenantId && u.Role == UserRole.Coach)
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
