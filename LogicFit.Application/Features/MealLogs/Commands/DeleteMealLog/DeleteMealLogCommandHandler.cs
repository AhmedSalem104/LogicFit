using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.MealLogs.Commands.DeleteMealLog;

public class DeleteMealLogCommandHandler : IRequestHandler<DeleteMealLogCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteMealLogCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteMealLogCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var clientId = Guid.Parse(_currentUserService.UserId!);

        var log = await _context.MealLogs
            .FirstOrDefaultAsync(l => l.Id == request.Id && l.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("MealLog", request.Id);

        // A client can only remove their own logs.
        if (log.ClientId != clientId)
            throw new ForbiddenException("You can only delete your own meal logs");

        _context.MealLogs.Remove(log);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
