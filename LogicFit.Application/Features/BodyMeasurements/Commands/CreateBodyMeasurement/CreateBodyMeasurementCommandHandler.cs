using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using MediatR;

namespace LogicFit.Application.Features.BodyMeasurements.Commands.CreateBodyMeasurement;

public class CreateBodyMeasurementCommandHandler : IRequestHandler<CreateBodyMeasurementCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IFileUploadService _fileUploadService;

    public CreateBodyMeasurementCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IFileUploadService fileUploadService)
    {
        _context = context;
        _tenantService = tenantService;
        _fileUploadService = fileUploadService;
    }

    public async Task<Guid> Handle(CreateBodyMeasurementCommand request, CancellationToken cancellationToken)
    {
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
            ClientId = request.ClientId,
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
