using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
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
    private readonly IRbacService _rbacService;

    public CreateClientCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        IRbacService rbacService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _rbacService = rbacService;
    }

    public async Task<Guid> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var phoneNumber = request.PhoneNumber.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email)
            ? $"{phoneNumber}@client.logicfit.com"
            : request.Email.Trim();

        // Check if phone number already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.PhoneNumber == phoneNumber, cancellationToken);

        if (existingUser != null)
            throw new ConflictException("Phone number already registered");

        // Email is unique per tenant. The UI keeps email optional, so use the same
        // deterministic fallback as the legacy flow instead of persisting an empty
        // string that would collide on the second client without an email.
        var existingEmail = await _context.Users
            .AnyAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);
        if (existingEmail)
            throw new ConflictException("Email already registered");

        // Auto-generate password if not provided (using phone number + random suffix)
        var password = request.Password ?? $"{request.PhoneNumber}@{Guid.NewGuid().ToString("N")[..6]}";

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PhoneNumber = phoneNumber,
            Email = email,
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

        // Assign the Client RBAC role (consistency with public registration).
        await _rbacService.EnsureUserInRoleAsync(user.Id, tenantId, SystemRoles.Client, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
