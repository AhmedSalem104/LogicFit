using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.ClientDashboard.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMyMeasurements;

public class GetMyMeasurementsQueryHandler : IRequestHandler<GetMyMeasurementsQuery, List<MyBodyMeasurementDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetMyMeasurementsQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<List<MyBodyMeasurementDto>> Handle(GetMyMeasurementsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var userId = Guid.Parse(_currentUserService.UserId!);

        return await _context.BodyMeasurements
            .Where(m => m.TenantId == tenantId && m.ClientId == userId)
            .OrderByDescending(m => m.DateRecorded)
            .Select(m => new MyBodyMeasurementDto
            {
                Id = m.Id,
                DateRecorded = m.DateRecorded,
                WeightKg = m.WeightKg,
                BodyFatPercent = m.BodyFatPercent,
                SkeletalMuscleMass = m.SkeletalMuscleMass
            })
            .ToListAsync(cancellationToken);
    }
}
