using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Branches.Commands.SetOperatingHours;

public class SetOperatingHoursCommandHandler : IRequestHandler<SetOperatingHoursCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public SetOperatingHoursCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Handle(SetOperatingHoursCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == request.BranchId && b.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Branch not found");

        var existing = await _context.BranchOperatingHours
            .Where(h => h.BranchId == branch.Id)
            .ToListAsync(cancellationToken);

        foreach (var h in existing)
            _context.BranchOperatingHours.Remove(h);

        foreach (var item in request.Hours)
        {
            _context.BranchOperatingHours.Add(new BranchOperatingHours
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BranchId = branch.Id,
                DayOfWeek = item.DayOfWeek,
                OpenTime = item.OpenTime,
                CloseTime = item.CloseTime,
                IsClosed = item.IsClosed
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
