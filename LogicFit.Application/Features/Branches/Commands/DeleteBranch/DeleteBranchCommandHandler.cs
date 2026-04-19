using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Branches.Commands.DeleteBranch;

public class DeleteBranchCommandHandler : IRequestHandler<DeleteBranchCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteBranchCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Branch not found");

        if (branch.IsDefault)
            throw new DomainException("Cannot delete the default branch");

        var hasSubscriptions = await _context.ClientSubscriptions.AnyAsync(s => s.BranchId == branch.Id, cancellationToken);
        if (hasSubscriptions)
            throw new DomainException("Cannot delete a branch that has linked subscriptions");

        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
