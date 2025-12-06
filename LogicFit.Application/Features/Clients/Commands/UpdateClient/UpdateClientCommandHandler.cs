using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Clients.Commands.UpdateClient;

public class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateClientCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var user = await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.TenantId == tenantId && u.Role == UserRole.Client, cancellationToken);

        if (user == null)
            throw new NotFoundException("Client", request.Id);

        // Check if phone number is being changed and if it's already taken
        if (user.PhoneNumber != request.PhoneNumber)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.PhoneNumber == request.PhoneNumber && u.Id != request.Id, cancellationToken);

            if (existingUser != null)
                throw new ConflictException("Phone number already registered");
        }

        user.Email = request.Email ?? user.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.IsActive = request.IsActive;

        // Update or create profile
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
