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
        var currentUserId = Guid.Parse(_currentUserService.UserId!);
        var currentUserRole = await _context.Users
            .Where(u => u.Id == currentUserId)
            .Select(u => u.Role)
            .FirstOrDefaultAsync(cancellationToken);

        var clientId = request.ClientId;
        if (currentUserRole == UserRole.Client)
        {
            if (request.ClientId != currentUserId)
                throw new ForbiddenException("Clients can only create measurements for themselves");

            clientId = currentUserId;
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
            TenantId = _tenantService.GetCurrentTenantId(),
            ClientId = clientId,
            DateRecorded = request.DateRecorded,
            WeightKg = request.WeightKg,
            SkeletalMuscleMass = request.SkeletalMuscleMass,
            BodyFatMass = request.BodyFatMass,
            BodyFatPercent = request.BodyFatPercent,
            TotalBodyWater = request.TotalBodyWater,
            Bmr = request.Bmr,
            VisceralFatLevel = request.VisceralFatLevel,
            InbodyImageUrl = inbodyImageUrl,
            FrontPhotoUrl = frontPhotoUrl,
            SidePhotoUrl = sidePhotoUrl,
            BackPhotoUrl = backPhotoUrl
        };

        _context.BodyMeasurements.Add(measurement);
        await _context.SaveChangesAsync(cancellationToken);

        return measurement.Id;
    }
}
