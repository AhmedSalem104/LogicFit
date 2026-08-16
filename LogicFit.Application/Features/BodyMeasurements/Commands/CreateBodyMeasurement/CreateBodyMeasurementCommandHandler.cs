using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.BodyMeasurements.Commands.CreateBodyMeasurement;

public class CreateBodyMeasurementCommandHandler : IRequestHandler<CreateBodyMeasurementCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IFileUploadService _fileUploadService;
    private readonly ICurrentUserService _currentUserService;

    public CreateBodyMeasurementCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IFileUploadService fileUploadService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _fileUploadService = fileUploadService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateBodyMeasurementCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            throw new ForbiddenException("An authenticated workspace user is required.");
        var currentUserRole = await _context.Users
            .Where(u => u.Id == currentUserId && u.TenantId == tenantId && u.IsActive)
            .Select(u => (UserRole?)u.Role)
            .FirstOrDefaultAsync(cancellationToken);

        if (!currentUserRole.HasValue)
            throw new ForbiddenException("The authenticated user is not active in this workspace.");

        Validate(request);

        var clientId = request.ClientId;
        if (currentUserRole == UserRole.Client)
        {
            if (request.ClientId != currentUserId)
                throw new ForbiddenException("Clients can only create measurements for themselves");

            clientId = currentUserId;
        }

        var clientExists = await _context.Users.AnyAsync(u => u.Id == clientId
            && u.TenantId == tenantId
            && u.Role == UserRole.Client
            && u.IsActive, cancellationToken);
        if (!clientExists)
            throw new NotFoundException("Client", clientId);

        if (currentUserRole is UserRole.Coach or UserRole.Trainer or UserRole.FreelanceCoach)
        {
            var assigned = await _context.CoachClients.AnyAsync(cc => cc.TenantId == tenantId
                && cc.CoachId == currentUserId
                && cc.ClientId == clientId
                && cc.IsActive
                && cc.UnassignedAt == null, cancellationToken);
            if (!assigned)
                throw new ForbiddenException("The client is not actively assigned to the current coach.");
        }

        string? inbodyImageUrl = null;
        string? frontPhotoUrl = null;
        string? sidePhotoUrl = null;
        string? backPhotoUrl = null;

        // Upload images if provided
        if (request.InbodyImage != null)
        {
            inbodyImageUrl = await _fileUploadService.UploadImageAsync(request.InbodyImage, "measurements");
        }

        if (request.FrontPhoto != null)
        {
            frontPhotoUrl = await _fileUploadService.UploadImageAsync(request.FrontPhoto, "measurements");
        }

        if (request.SidePhoto != null)
        {
            sidePhotoUrl = await _fileUploadService.UploadImageAsync(request.SidePhoto, "measurements");
        }

        if (request.BackPhoto != null)
        {
            backPhotoUrl = await _fileUploadService.UploadImageAsync(request.BackPhoto, "measurements");
        }

        var measurement = new BodyMeasurement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = clientId,
            DateRecorded = request.DateRecorded == default ? DateTime.UtcNow.Date : request.DateRecorded.Date,
            WeightKg = request.WeightKg,
            HeightCm = request.HeightCm,
            ChestCm = request.ChestCm,
            WaistCm = request.WaistCm,
            HipsCm = request.HipsCm,
            ArmsCm = request.ArmsCm,
            ThighsCm = request.ThighsCm,
            SkeletalMuscleMass = request.SkeletalMuscleMass,
            BodyFatMass = request.BodyFatMass,
            BodyFatPercent = request.BodyFatPercent,
            TotalBodyWater = request.TotalBodyWater,
            Bmr = request.Bmr,
            VisceralFatLevel = request.VisceralFatLevel,
            Notes = request.Notes?.Trim(),
            InbodyImageUrl = inbodyImageUrl,
            FrontPhotoUrl = frontPhotoUrl,
            SidePhotoUrl = sidePhotoUrl,
            BackPhotoUrl = backPhotoUrl
        };

        _context.BodyMeasurements.Add(measurement);
        await _context.SaveChangesAsync(cancellationToken);

        return measurement.Id;
    }

    private static void Validate(CreateBodyMeasurementCommand request)
    {
        if (request.WeightKg < 0 || request.WeightKg > 1000) throw new ValidationException("WeightKg", "Weight must be between 0 and 1000 kg.");
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
