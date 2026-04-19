using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Maintenance.Commands.ResolveMaintenance;

public class ResolveMaintenanceCommandHandler : IRequestHandler<ResolveMaintenanceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public ResolveMaintenanceCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task Handle(ResolveMaintenanceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var record = await _context.MaintenanceRecords
            .Include(r => r.Equipment)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("MaintenanceRecord", request.Id);

        if (record.Status == MaintenanceStatus.Completed)
            throw new DomainException("Maintenance record is already completed");

        record.Status = MaintenanceStatus.Completed;
        record.ResolvedDate = _dateTimeService.UtcNow;
        if (!string.IsNullOrEmpty(request.ResolutionNotes))
            record.ResolutionNotes = request.ResolutionNotes;
        if (request.FinalCost.HasValue)
            record.Cost = request.FinalCost.Value;

        if (request.ReactivateEquipment && record.Equipment.Status == EquipmentStatus.UnderMaintenance)
            record.Equipment.Status = EquipmentStatus.Active;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
