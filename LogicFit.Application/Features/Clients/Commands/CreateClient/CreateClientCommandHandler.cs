using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Clients.Commands.CreateClient;

public class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public CreateClientCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Check if phone number already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.PhoneNumber == request.PhoneNumber, cancellationToken);

        if (existingUser != null)
            throw new ConflictException("Phone number already registered");

        // Auto-generate password if not provided (using phone number + random suffix)
        var password = request.Password ?? $"{request.PhoneNumber}@{Guid.NewGuid().ToString("N")[..6]}";

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email ?? $"{request.PhoneNumber}@client.logicfit.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Client,
            IsActive = true,
            WalletBalance = 0
        };

        _context.Users.Add(user);

        // Create profile if any profile data provided
        if (!string.IsNullOrEmpty(request.FullName) || request.Gender.HasValue ||
            request.BirthDate.HasValue || request.HeightCm.HasValue)
        {
            var profile = new UserProfile
            {
                UserId = user.Id,
                FullName = request.FullName,
                Gender = request.Gender.HasValue ? (GenderType)request.Gender.Value : null,
                BirthDate = request.BirthDate,
                HeightCm = request.HeightCm,
                ActivityLevel = request.ActivityLevel,
                MedicalHistory = request.MedicalHistory
            };
            _context.UserProfiles.Add(profile);
        }

        // Auto-assign to coach if CoachId provided or if current user is a coach
        Guid? coachId = request.CoachId;
        if (!coachId.HasValue && !string.IsNullOrEmpty(_currentUserService.UserId) &&
            Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            coachId = currentUserId;
        }

        if (coachId.HasValue)
        {
            var coach = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == coachId.Value && u.TenantId == tenantId &&
                                         (u.Role == UserRole.Coach || u.Role == UserRole.Owner), cancellationToken);

            if (coach != null)
            {
                var coachClient = new CoachClient
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CoachId = coach.Id,
                    ClientId = user.Id,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.CoachClients.Add(coachClient);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
