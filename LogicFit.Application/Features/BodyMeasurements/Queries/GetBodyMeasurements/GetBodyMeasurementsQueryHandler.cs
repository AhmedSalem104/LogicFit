using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.BodyMeasurements.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.BodyMeasurements.Queries.GetBodyMeasurements;

public class GetBodyMeasurementsQueryHandler : IRequestHandler<GetBodyMeasurementsQuery, List<BodyMeasurementDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetBodyMeasurementsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<BodyMeasurementDto>> Handle(GetBodyMeasurementsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.BodyMeasurements
            .Include(b => b.Client).ThenInclude(c => c.Profile)
            .Where(b => b.TenantId == tenantId)
            .AsQueryable();

        if (request.ClientId.HasValue)
            query = query.Where(b => b.ClientId == request.ClientId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(b => b.DateRecorded >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(b => b.DateRecorded <= request.ToDate.Value);

        return await query
            .OrderByDescending(b => b.DateRecorded)
            .Select(b => new BodyMeasurementDto
            {
                Id = b.Id,
                TenantId = b.TenantId,
                ClientId = b.ClientId,
                ClientName = b.Client.Profile != null ? b.Client.Profile.FullName : b.Client.Email,
                DateRecorded = b.DateRecorded,
                WeightKg = b.WeightKg,
                SkeletalMuscleMass = b.SkeletalMuscleMass,
                BodyFatMass = b.BodyFatMass,
                BodyFatPercent = b.BodyFatPercent,
                TotalBodyWater = b.TotalBodyWater,
                Bmr = b.Bmr,
                VisceralFatLevel = b.VisceralFatLevel,
                InbodyImageUrl = b.InbodyImageUrl,
                FrontPhotoUrl = b.FrontPhotoUrl,
                SidePhotoUrl = b.SidePhotoUrl,
                BackPhotoUrl = b.BackPhotoUrl
            })
            .ToListAsync(cancellationToken);
    }
}
