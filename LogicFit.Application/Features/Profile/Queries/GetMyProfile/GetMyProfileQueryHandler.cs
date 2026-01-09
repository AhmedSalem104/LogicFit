using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Users.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Profile.Queries.GetMyProfile;

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, UserDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyProfileQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<UserDto?> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId))
            return null;

        var userId = Guid.Parse(_currentUserService.UserId);

        return await _context.Users
            .Include(u => u.Profile)
            .Where(u => u.Id == userId)
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
                    ProfilePictureUrl = u.Profile.ProfilePictureUrl,
                    Gender = (int?)u.Profile.Gender,
                    BirthDate = u.Profile.BirthDate,
                    HeightCm = u.Profile.HeightCm,
                    WeightKg = u.Profile.WeightKg,
                    ActivityLevel = u.Profile.ActivityLevel,
                    FitnessGoal = u.Profile.FitnessGoal,
                    MedicalHistory = u.Profile.MedicalHistory
                } : null
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
