using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Users.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetUserByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        return await _context.Users
            .Include(u => u.Profile)
            .Where(u => u.Id == request.Id && u.TenantId == tenantId)
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
