using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Equipment.Commands.ChangeEquipmentStatus;

public class ChangeEquipmentStatusCommandHandler : IRequestHandler<ChangeEquipmentStatusCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public ChangeEquipmentStatusCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(ChangeEquipmentStatusCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var equipment = await _context.Equipment
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Equipment", request.Id);

        equipment.Status = request.Status;
        if (!string.IsNullOrEmpty(request.Notes))
            equipment.Notes = request.Notes;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
