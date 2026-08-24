using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.AthleteCheckins.Commands.CreateAthleteCheckin;

public sealed class CreateAthleteCheckinCommandHandler : IRequestHandler<CreateAthleteCheckinCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUser;

    public CreateAthleteCheckinCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUser)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateAthleteCheckinCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var currentUserId = await EnsureCanAccessClientAsync(request.ClientId, tenantId, cancellationToken);
        Validate(request);
        var date = request.CheckinDate == default ? DateTime.UtcNow.Date : request.CheckinDate.Date;

        if (await _context.AthleteCheckins.AnyAsync(x => x.TenantId == tenantId && x.ClientId == request.ClientId && x.CheckinDate == date, cancellationToken))
            throw new ConflictException("A daily check-in already exists for this client and date.");

        var checkin = new AthleteCheckin
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = request.ClientId,
            CheckinDate = date,
            SleepHours = request.SleepHours,
            SleepQuality = request.SleepQuality,
            Fatigue = request.Fatigue,
            Soreness = request.Soreness,
            Stress = request.Stress,
            Mood = request.Mood,
            RestingHeartRate = request.RestingHeartRate,
            Hrv = request.Hrv,
            BodyweightKg = request.BodyweightKg,
            Notes = request.Notes?.Trim()
        };

        _context.AthleteCheckins.Add(checkin);
        await _context.SaveChangesAsync(cancellationToken);
        return checkin.Id;
    }

    private async Task<Guid> EnsureCanAccessClientAsync(Guid clientId, Guid tenantId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated workspace user is required.");

        var role = await _context.Users
            .Where(x => x.Id == currentUserId && x.TenantId == tenantId && x.IsActive)
            .Select(x => (UserRole?)x.Role)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("The authenticated user is not active in this workspace.");

        if (!await _context.Users.AnyAsync(x => x.Id == clientId && x.TenantId == tenantId && x.Role == UserRole.Client && x.IsActive, cancellationToken))
            throw new NotFoundException("Client", clientId);

        if (role == UserRole.Client && currentUserId != clientId)
            throw new ForbiddenException("Clients can only create their own check-ins.");

        if (role is UserRole.Coach or UserRole.Trainer or UserRole.FreelanceCoach)
        {
            var assigned = await _context.CoachClients.AnyAsync(x => x.TenantId == tenantId && x.CoachId == currentUserId && x.ClientId == clientId && x.IsActive && x.UnassignedAt == null, cancellationToken);
            if (!assigned)
                throw new ForbiddenException("The client is not actively assigned to the current coach.");
        }
        else if (role is not (UserRole.Client or UserRole.Owner or UserRole.Manager or UserRole.FreelanceOwner))
        {
            throw new ForbiddenException("You cannot manage coaching check-ins.");
        }

        return currentUserId;
    }

    private static void Validate(CreateAthleteCheckinCommand request)
    {
        if (request.SleepHours is < 0 or > 24) throw new ValidationException("SleepHours", "Sleep hours must be between 0 and 24.");
        ValidateScale(request.SleepQuality, nameof(request.SleepQuality));
        ValidateScale(request.Fatigue, nameof(request.Fatigue));
        ValidateScale(request.Soreness, nameof(request.Soreness));
        ValidateScale(request.Stress, nameof(request.Stress));
        ValidateScale(request.Mood, nameof(request.Mood));
        if (request.RestingHeartRate is < 20 or > 250) throw new ValidationException("RestingHeartRate", "Resting heart rate must be between 20 and 250.");
        if (request.Hrv is < 0 or > 500) throw new ValidationException("Hrv", "HRV must be between 0 and 500.");
        if (request.BodyweightKg is < 0 or > 1000) throw new ValidationException("BodyweightKg", "Body weight must be between 0 and 1000 kg.");
        if (request.Notes?.Length > 1000) throw new ValidationException("Notes", "Notes cannot exceed 1000 characters.");
    }

    private static void ValidateScale(int? value, string field)
    {
        if (value is < 1 or > 5) throw new ValidationException(field, "The value must be between 1 and 5.");
    }
}
