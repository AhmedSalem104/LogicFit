using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.BodyMeasurements.Commands.DeleteBodyMeasurement;

public class DeleteBodyMeasurementCommandHandler : IRequestHandler<DeleteBodyMeasurementCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteBodyMeasurementCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(DeleteBodyMeasurementCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var measurement = await _context.BodyMeasurements
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.TenantId == tenantId, cancellationToken);

        if (measurement == null)
            throw new NotFoundException("BodyMeasurement", request.Id);

        _context.BodyMeasurements.Remove(measurement);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
