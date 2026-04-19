using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EquipmentEntity = LogicFit.Domain.Entities.Equipment;

namespace LogicFit.Application.Features.Equipment.Commands.CreateEquipment;

public class CreateEquipmentCommandHandler : IRequestHandler<CreateEquipmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateEquipmentCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateEquipmentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var branchExists = await _context.Branches
            .AnyAsync(b => b.Id == request.BranchId && b.TenantId == tenantId, cancellationToken);
        if (!branchExists)
            throw new NotFoundException("Branch", request.BranchId);

        if (request.RoomId.HasValue)
        {
            var roomValid = await _context.Rooms
                .AnyAsync(r => r.Id == request.RoomId.Value && r.TenantId == tenantId && r.BranchId == request.BranchId, cancellationToken);
            if (!roomValid)
                throw new DomainException("Room does not belong to the specified branch");
        }

        if (!string.IsNullOrEmpty(request.SerialNumber))
        {
            var duplicate = await _context.Equipment
                .AnyAsync(e => e.TenantId == tenantId && e.SerialNumber == request.SerialNumber, cancellationToken);
            if (duplicate)
                throw new ConflictException("Serial number already exists");
        }

        var equipment = new EquipmentEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = request.BranchId,
            RoomId = request.RoomId,
            Name = request.Name,
            SerialNumber = request.SerialNumber,
            Brand = request.Brand,
            Model = request.Model,
            Category = request.Category,
            PurchaseDate = request.PurchaseDate,
            PurchasePrice = request.PurchasePrice,
            Status = request.Status,
            WarrantyUntil = request.WarrantyUntil,
            ImageUrl = request.ImageUrl,
            Notes = request.Notes
        };

        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync(cancellationToken);

        return equipment.Id;
    }
}
