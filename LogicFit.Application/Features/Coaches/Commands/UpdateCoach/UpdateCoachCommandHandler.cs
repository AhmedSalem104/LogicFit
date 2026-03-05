using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Coaches.Commands.UpdateCoach;

public class UpdateCoachCommandHandler : IRequestHandler<UpdateCoachCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateCoachCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateCoachCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var user = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.TenantId == tenantId && u.Role == UserRole.Coach, cancellationToken);

        if (user == null)
            throw new NotFoundException("Coach", request.Id);

        // Check if phone number is being changed and if it's already taken
        if (!string.IsNullOrEmpty(request.PhoneNumber) && user.PhoneNumber != request.PhoneNumber)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.PhoneNumber == request.PhoneNumber && u.Id != request.Id, cancellationToken);

            if (existingUser != null)
                throw new ConflictException("Phone number already registered");

            user.PhoneNumber = request.PhoneNumber;
        }

        if (request.Email != null)
            user.Email = request.Email;

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        // Update or create profile
        if (user.Profile == null)
        {
            user.Profile = new UserProfile
            {
                UserId = user.Id
            };
            _context.UserProfiles.Add(user.Profile);
        }

        if (request.FullName != null)
            user.Profile.FullName = request.FullName;

        if (request.Gender.HasValue)
            user.Profile.Gender = (GenderType)request.Gender.Value;

        if (request.BirthDate.HasValue)
            user.Profile.BirthDate = request.BirthDate;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
