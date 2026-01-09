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
    private readonly ICurrentUserService _currentUserService;

    public UpdateMyProfileCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId))
            throw new UnauthorizedAccessException("User not authenticated");

        var userId = Guid.Parse(_currentUserService.UserId);

        var user = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new NotFoundException("User", userId);

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
