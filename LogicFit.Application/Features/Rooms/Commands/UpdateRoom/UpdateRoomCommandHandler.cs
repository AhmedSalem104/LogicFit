using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Rooms.Commands.UpdateRoom;

public class UpdateRoomCommandHandler : IRequestHandler<UpdateRoomCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateRoomCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(UpdateRoomCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Room", request.Id);

        room.Name = request.Name;
        room.Type = request.Type;
        room.Capacity = request.Capacity;
        room.Description = request.Description;
        room.ImageUrl = request.ImageUrl;
        room.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
