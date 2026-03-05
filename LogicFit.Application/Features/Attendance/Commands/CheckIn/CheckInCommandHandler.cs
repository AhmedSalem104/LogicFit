using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Attendance.Commands.CheckIn;

public class CheckInCommandHandler : IRequestHandler<CheckInCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CheckInCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // Verify client exists
        var clientExists = await _context.Users
            .AnyAsync(u => u.Id == request.ClientId && u.TenantId == tenantId && !u.IsDeleted, cancellationToken);

        if (!clientExists)
            throw new NotFoundException("Client", request.ClientId);

        // Check no open check-in already exists
        var openCheckIn = await _context.Attendances
            .AnyAsync(a => a.ClientId == request.ClientId && a.TenantId == tenantId
                && a.CheckOutTime == null && !a.IsDeleted, cancellationToken);

        if (openCheckIn)
            throw new ConflictException("Client already has an open check-in. Please check out first.");

        var attendance = new Domain.Entities.Attendance
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = request.ClientId,
            CheckInTime = DateTime.UtcNow,
            Notes = request.Notes
        };

        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync(cancellationToken);

        return attendance.Id;
    }
}
