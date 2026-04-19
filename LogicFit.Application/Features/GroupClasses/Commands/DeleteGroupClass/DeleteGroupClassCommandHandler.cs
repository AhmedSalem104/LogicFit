using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.GroupClasses.Commands.DeleteGroupClass;

public class DeleteGroupClassCommandHandler : IRequestHandler<DeleteGroupClassCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteGroupClassCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(DeleteGroupClassCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var gc = await _context.GroupClasses
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("GroupClass", request.Id);

        _context.GroupClasses.Remove(gc);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
