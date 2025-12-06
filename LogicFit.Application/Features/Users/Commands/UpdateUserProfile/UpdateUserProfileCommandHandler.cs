using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateUserProfileCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var user = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.TenantId == tenantId, cancellationToken);

        if (user == null)
            throw new NotFoundException("User", request.UserId);

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
        user.Profile.ActivityLevel = request.ActivityLevel;
        user.Profile.MedicalHistory = request.MedicalHistory;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
