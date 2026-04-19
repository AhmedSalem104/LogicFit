using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Maintenance.Commands.CreateMaintenance;

public class CreateMaintenanceCommandHandler : IRequestHandler<CreateMaintenanceCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public CreateMaintenanceCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Guid> Handle(CreateMaintenanceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var equipment = await _context.Equipment
            .FirstOrDefaultAsync(e => e.Id == request.EquipmentId && e.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Equipment", request.EquipmentId);

        var record = new MaintenanceRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EquipmentId = equipment.Id,
            IssueDate = request.IssueDate ?? _dateTimeService.UtcNow,
            Description = request.Description,
            TechnicianName = request.TechnicianName,
            TechnicianContact = request.TechnicianContact,
            Cost = request.Cost,
            Status = MaintenanceStatus.Pending
        };

        _context.MaintenanceRecords.Add(record);

        if (request.PutEquipmentUnderMaintenance)
            equipment.Status = EquipmentStatus.UnderMaintenance;

        await _context.SaveChangesAsync(cancellationToken);

        return record.Id;
    }
}
