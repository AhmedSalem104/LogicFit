using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.CoachClients.Commands.AddTrainee;

public class AddTraineeCommandHandler : IRequestHandler<AddTraineeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public AddTraineeCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(AddTraineeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Get current user (coach)
        if (string.IsNullOrEmpty(_currentUserService.UserId) ||
            !Guid.TryParse(_currentUserService.UserId, out var coachId))
        {
            throw new UnauthorizedException("User not authenticated");
        }

        // Verify current user is a coach or owner
        var coach = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == coachId && u.TenantId == tenantId &&
                                     (u.Role == UserRole.Coach || u.Role == UserRole.Owner), cancellationToken);

        if (coach == null)
            throw new ForbiddenException("Only coaches or owners can add trainees");

        // Check if phone number already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.PhoneNumber == request.ClientPhone, cancellationToken);

        if (existingUser != null)
            throw new ConflictException("Phone number already registered");

        // Auto-generate password
        var password = $"{request.ClientPhone}@{Guid.NewGuid().ToString("N")[..6]}";

        // Create new client
        var client = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PhoneNumber = request.ClientPhone,
            Email = request.ClientEmail ?? $"{request.ClientPhone}@client.logicfit.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Client,
            IsActive = true,
            WalletBalance = 0
        };

        _context.Users.Add(client);

        // Create profile
        var profile = new UserProfile
        {
            UserId = client.Id,
            FullName = request.ClientName,
            Gender = request.Gender.HasValue ? (GenderType)request.Gender.Value : null,
            BirthDate = request.BirthDate,
            HeightCm = request.HeightCm,
            ActivityLevel = request.ActivityLevel,
            MedicalHistory = request.MedicalHistory
        };
        _context.UserProfiles.Add(profile);

        // Assign to coach
        var coachClient = new CoachClient
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CoachId = coachId,
            ClientId = client.Id,
            AssignedAt = DateTime.UtcNow,
            IsActive = true,
            Notes = request.Notes
        };
        _context.CoachClients.Add(coachClient);

        await _context.SaveChangesAsync(cancellationToken);

        return client.Id;
    }
}
