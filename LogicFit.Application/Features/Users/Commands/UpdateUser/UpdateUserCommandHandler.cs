using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateUserCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.TenantId == tenantId, cancellationToken);

        if (user == null)
            throw new NotFoundException("User", request.Id);

        user.PhoneNumber = request.PhoneNumber;
        user.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
