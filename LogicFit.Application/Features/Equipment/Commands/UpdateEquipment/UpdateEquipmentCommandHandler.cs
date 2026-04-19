using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Equipment.Commands.UpdateEquipment;

public class UpdateEquipmentCommandHandler : IRequestHandler<UpdateEquipmentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateEquipmentCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(UpdateEquipmentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var equipment = await _context.Equipment
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Equipment", request.Id);

        if (request.RoomId.HasValue && request.RoomId != equipment.RoomId)
        {
            var valid = await _context.Rooms
                .AnyAsync(r => r.Id == request.RoomId.Value && r.TenantId == tenantId && r.BranchId == equipment.BranchId, cancellationToken);
            if (!valid)
                throw new DomainException("Room does not belong to the equipment's branch");
        }

        if (!string.IsNullOrEmpty(request.SerialNumber) && request.SerialNumber != equipment.SerialNumber)
        {
            var duplicate = await _context.Equipment
                .AnyAsync(e => e.TenantId == tenantId && e.SerialNumber == request.SerialNumber && e.Id != equipment.Id, cancellationToken);
            if (duplicate)
                throw new ConflictException("Serial number already exists");
        }

        equipment.RoomId = request.RoomId;
        equipment.Name = request.Name;
        equipment.SerialNumber = request.SerialNumber;
        equipment.Brand = request.Brand;
        equipment.Model = request.Model;
        equipment.Category = request.Category;
        equipment.PurchaseDate = request.PurchaseDate;
        equipment.PurchasePrice = request.PurchasePrice;
        equipment.Status = request.Status;
        equipment.WarrantyUntil = request.WarrantyUntil;
        equipment.ImageUrl = request.ImageUrl;
        equipment.Notes = request.Notes;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
