using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.AthleteCheckins.Commands.CreateAthleteCheckin;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.AthleteCheckins.Commands.UpdateAthleteCheckin;

public sealed class UpdateAthleteCheckinCommandHandler : IRequestHandler<UpdateAthleteCheckinCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUser;

    public UpdateAthleteCheckinCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUser)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(UpdateAthleteCheckinCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated workspace user is required.");

        var checkin = await _context.AthleteCheckins.FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("AthleteCheckin", request.Id);
        await EnsureCanAccessClientAsync(checkin.ClientId, currentUserId, tenantId, cancellationToken);

        var create = new CreateAthleteCheckinCommand
        {
            SleepHours = request.SleepHours,
            SleepQuality = request.SleepQuality,
            Fatigue = request.Fatigue,
            Soreness = request.Soreness,
            Stress = request.Stress,
            Mood = request.Mood,
            RestingHeartRate = request.RestingHeartRate,
            Hrv = request.Hrv,
            BodyweightKg = request.BodyweightKg,
            Notes = request.Notes
        };
        Validate(create);

        checkin.SleepHours = request.SleepHours;
        checkin.SleepQuality = request.SleepQuality;
        checkin.Fatigue = request.Fatigue;
        checkin.Soreness = request.Soreness;
        checkin.Stress = request.Stress;
        checkin.Mood = request.Mood;
        checkin.RestingHeartRate = request.RestingHeartRate;
        checkin.Hrv = request.Hrv;
        checkin.BodyweightKg = request.BodyweightKg;
        checkin.Notes = request.Notes?.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureCanAccessClientAsync(Guid clientId, Guid currentUserId, Guid tenantId, CancellationToken cancellationToken)
    {
        var role = await _context.Users.Where(x => x.Id == currentUserId && x.TenantId == tenantId && x.IsActive)
            .Select(x => (UserRole?)x.Role).FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("The authenticated user is not active in this workspace.");
        if (role == UserRole.Client && currentUserId != clientId)
            throw new ForbiddenException("Clients can only edit their own check-ins.");
        if (role is UserRole.Coach or UserRole.Trainer or UserRole.FreelanceCoach)
        {
            if (!await _context.CoachClients.AnyAsync(x => x.TenantId == tenantId && x.CoachId == currentUserId && x.ClientId == clientId && x.IsActive && x.UnassignedAt == null, cancellationToken))
                throw new ForbiddenException("The client is not actively assigned to the current coach.");
        }
        else if (role is not (UserRole.Client or UserRole.Owner or UserRole.Manager or UserRole.FreelanceOwner))
            throw new ForbiddenException("You cannot manage coaching check-ins.");
    }

    private static void Validate(CreateAthleteCheckinCommand request)
    {
        if (request.SleepHours is < 0 or > 24) throw new ValidationException("SleepHours", "Sleep hours must be between 0 and 24.");
        foreach (var (value, field) in new[] { (request.SleepQuality, "SleepQuality"), (request.Fatigue, "Fatigue"), (request.Soreness, "Soreness"), (request.Stress, "Stress"), (request.Mood, "Mood") })
            if (value is < 1 or > 5) throw new ValidationException(field, "The value must be between 1 and 5.");
        if (request.RestingHeartRate is < 20 or > 250) throw new ValidationException("RestingHeartRate", "Resting heart rate must be between 20 and 250.");
        if (request.Hrv is < 0 or > 500) throw new ValidationException("Hrv", "HRV must be between 0 and 500.");
        if (request.BodyweightKg is < 0 or > 1000) throw new ValidationException("BodyweightKg", "Body weight must be between 0 and 1000 kg.");
        if (request.Notes?.Length > 1000) throw new ValidationException("Notes", "Notes cannot exceed 1000 characters.");
    }
}
