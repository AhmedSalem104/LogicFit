using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.BodyMeasurements.Commands.UpdateBodyMeasurement;

public sealed class UpdateBodyMeasurementCommandHandler : IRequestHandler<UpdateBodyMeasurementCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUser;

    public UpdateBodyMeasurementCommandHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUser)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(UpdateBodyMeasurementCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated workspace user is required.");
        var measurement = await _context.BodyMeasurements.FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("BodyMeasurement", request.Id);
        await EnsureCanAccessAsync(measurement.ClientId, currentUserId, tenantId, cancellationToken);
        Validate(request);

        if (request.WeightKg.HasValue) measurement.WeightKg = request.WeightKg.Value;
        measurement.HeightCm = request.HeightCm;
        measurement.ChestCm = request.ChestCm;
        measurement.WaistCm = request.WaistCm;
        measurement.HipsCm = request.HipsCm;
        measurement.ArmsCm = request.ArmsCm;
        measurement.ThighsCm = request.ThighsCm;
        measurement.SkeletalMuscleMass = request.SkeletalMuscleMass;
        measurement.BodyFatMass = request.BodyFatMass;
        measurement.BodyFatPercent = request.BodyFatPercent;
        measurement.TotalBodyWater = request.TotalBodyWater;
        measurement.Bmr = request.Bmr;
        measurement.VisceralFatLevel = request.VisceralFatLevel;
        measurement.InbodyImageUrl = request.InbodyImageUrl ?? measurement.InbodyImageUrl;
        measurement.FrontPhotoUrl = request.FrontPhotoUrl ?? measurement.FrontPhotoUrl;
        measurement.SidePhotoUrl = request.SidePhotoUrl ?? measurement.SidePhotoUrl;
        measurement.BackPhotoUrl = request.BackPhotoUrl ?? measurement.BackPhotoUrl;
        measurement.Notes = request.Notes?.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureCanAccessAsync(Guid clientId, Guid currentUserId, Guid tenantId, CancellationToken cancellationToken)
    {
        var role = await _context.Users.Where(x => x.Id == currentUserId && x.TenantId == tenantId && x.IsActive)
            .Select(x => (UserRole?)x.Role).FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("The authenticated user is not active in this workspace.");
        if (role == UserRole.Client && currentUserId != clientId)
            throw new ForbiddenException("Clients can only edit their own measurements.");
        if (role is UserRole.Coach or UserRole.Trainer or UserRole.FreelanceCoach)
        {
            if (!await _context.CoachClients.AnyAsync(x => x.TenantId == tenantId && x.CoachId == currentUserId && x.ClientId == clientId && x.IsActive && x.UnassignedAt == null, cancellationToken))
                throw new ForbiddenException("The client is not actively assigned to the current coach.");
        }
        else if (role is not (UserRole.Client or UserRole.Owner or UserRole.Manager or UserRole.FreelanceOwner))
            throw new ForbiddenException("You cannot manage measurements.");
    }

    private static void Validate(UpdateBodyMeasurementCommand request)
    {
        if (request.WeightKg is < 0 or > 1000) throw new ValidationException("WeightKg", "Weight must be between 0 and 1000 kg.");
        foreach (var (value, field) in new[]
        {
            (request.HeightCm, "HeightCm"), (request.ChestCm, "ChestCm"), (request.WaistCm, "WaistCm"),
            (request.HipsCm, "HipsCm"), (request.ArmsCm, "ArmsCm"), (request.ThighsCm, "ThighsCm")
        })
            if (value is < 0 or > 500) throw new ValidationException(field, "The measurement must be between 0 and 500 cm.");
        if (request.BodyFatPercent is < 0 or > 100) throw new ValidationException("BodyFatPercent", "Body fat percentage must be between 0 and 100.");
        if (request.Notes?.Length > 1000) throw new ValidationException("Notes", "Notes cannot exceed 1000 characters.");
    }
}
