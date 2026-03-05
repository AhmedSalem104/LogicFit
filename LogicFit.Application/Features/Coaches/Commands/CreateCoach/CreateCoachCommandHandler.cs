using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Coaches.Commands.CreateCoach;

public class CreateCoachCommandHandler : IRequestHandler<CreateCoachCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateCoachCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateCoachCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Check if phone number already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.PhoneNumber == request.PhoneNumber, cancellationToken);

        if (existingUser != null)
            throw new ConflictException("Phone number already registered");

        // Auto-generate password if not provided
        var password = request.Password ?? $"{request.PhoneNumber}@{Guid.NewGuid().ToString("N")[..6]}";

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email ?? $"{request.PhoneNumber}@coach.logicfit.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Coach,
            IsActive = true,
            WalletBalance = 0
        };

        _context.Users.Add(user);

        // Create profile if any profile data provided
        if (!string.IsNullOrEmpty(request.FullName) || request.Gender.HasValue || request.BirthDate.HasValue)
        {
            var profile = new UserProfile
            {
                UserId = user.Id,
                FullName = request.FullName,
                Gender = request.Gender.HasValue ? (GenderType)request.Gender.Value : null,
                BirthDate = request.BirthDate
            };
            _context.UserProfiles.Add(profile);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
