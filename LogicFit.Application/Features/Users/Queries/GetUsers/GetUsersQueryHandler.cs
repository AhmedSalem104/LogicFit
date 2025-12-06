using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Users.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetUsersQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Users
            .Include(u => u.Profile)
            .Where(u => u.TenantId == tenantId)
            .AsQueryable();

        if (request.Role.HasValue)
            query = query.Where(u => u.Role == request.Role.Value);

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query = query.Where(u => u.Email.Contains(request.SearchTerm) ||
                                     (u.Profile != null && u.Profile.FullName != null && u.Profile.FullName.Contains(request.SearchTerm)));

        return await query
            .Select(u => new UserDto
            {
                Id = u.Id,
                TenantId = u.TenantId,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role,
                IsActive = u.IsActive,
                WalletBalance = u.WalletBalance,
                Profile = u.Profile != null ? new UserProfileDto
                {
                    FullName = u.Profile.FullName,
                    Gender = (int?)u.Profile.Gender,
                    BirthDate = u.Profile.BirthDate,
                    HeightCm = u.Profile.HeightCm,
                    ActivityLevel = u.Profile.ActivityLevel,
                    MedicalHistory = u.Profile.MedicalHistory
                } : null
            })
            .ToListAsync(cancellationToken);
    }
}
