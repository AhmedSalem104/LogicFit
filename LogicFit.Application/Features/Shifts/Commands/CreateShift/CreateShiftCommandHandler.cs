using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using MediatR;

namespace LogicFit.Application.Features.Shifts.Commands.CreateShift;

public class CreateShiftCommandHandler : IRequestHandler<CreateShiftCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateShiftCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CreateShiftCommand request, CancellationToken cancellationToken)
    {
        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.GetCurrentTenantId(),
            BranchId = request.BranchId,
            Name = request.Name,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Color = request.Color,
            IsActive = request.IsActive
        };
        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync(cancellationToken);
        return shift.Id;
    }
}
