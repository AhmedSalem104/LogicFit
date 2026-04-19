using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.GroupClasses.Commands.UpdateGroupClass;

public class UpdateGroupClassCommandHandler : IRequestHandler<UpdateGroupClassCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateGroupClassCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(UpdateGroupClassCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var gc = await _context.GroupClasses
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("GroupClass", request.Id);

        gc.BranchId = request.BranchId;
        gc.Name = request.Name;
        gc.Description = request.Description;
        gc.Category = request.Category;
        gc.DurationMinutes = request.DurationMinutes;
        gc.Capacity = request.Capacity;
        gc.Color = request.Color;
        gc.ImageUrl = request.ImageUrl;
        gc.Price = request.Price;
        gc.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
