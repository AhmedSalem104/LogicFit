using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Foods.Commands.DeleteFood;

public class DeleteFoodCommandHandler : IRequestHandler<DeleteFoodCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteFoodCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(DeleteFoodCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var food = await _context.Foods
            .FirstOrDefaultAsync(f => f.Id == request.Id && f.TenantId == tenantId, cancellationToken);

        if (food == null)
            throw new NotFoundException("Food", request.Id);

        _context.Foods.Remove(food);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
