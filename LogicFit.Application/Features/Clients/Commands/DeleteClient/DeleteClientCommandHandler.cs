using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Clients.Commands.DeleteClient;

public class DeleteClientCommandHandler : IRequestHandler<DeleteClientCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteClientCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.TenantId == tenantId && u.Role == UserRole.Client, cancellationToken);

        if (user == null)
            throw new NotFoundException("Client", request.Id);

        // Soft delete - just deactivate
        user.IsActive = false;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
