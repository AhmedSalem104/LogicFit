using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Profile.Commands.UpdateMyProfile;

public class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMyProfileCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId))
            throw new UnauthorizedAccessException("User not authenticated");

        var userId = Guid.Parse(_currentUserService.UserId);
        var tenantId = _tenantService.GetCurrentTenantId();

        var user = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, cancellationToken);

        if (user == null)
            throw new NotFoundException("User", userId);

        if (request.PhoneNumber != null && request.PhoneNumber != user.PhoneNumber)
        {
            var phoneInUse = await _context.Users.AnyAsync(u => u.TenantId == tenantId
                && u.PhoneNumber == request.PhoneNumber
                && u.Id != userId, cancellationToken);
            if (phoneInUse)
                throw new ConflictException("Phone number is already registered in this workspace.");

            user.PhoneNumber = request.PhoneNumber;
        }

        if (user.Profile == null)
        {
            user.Profile = new UserProfile
            {
                UserId = user.Id
            };
            _context.UserProfiles.Add(user.Profile);
        }

        user.Profile.FullName = request.FullName;
        user.Profile.Gender = request.Gender.HasValue ? (GenderType)request.Gender.Value : null;
        user.Profile.BirthDate = request.BirthDate;
        user.Profile.HeightCm = request.HeightCm;
        user.Profile.WeightKg = request.WeightKg;
        user.Profile.ActivityLevel = request.ActivityLevel;
        user.Profile.FitnessGoal = request.FitnessGoal;
        user.Profile.MedicalHistory = request.MedicalHistory;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
