using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Appointments.Commands.CreateAppointment;

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public CreateAppointmentCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        // If CoachId not provided, use current user as coach
        var coachId = request.CoachId ?? Guid.Parse(_currentUserService.UserId!);

        // Validate coach exists
        var coachExists = await _context.Users
            .AnyAsync(u => u.Id == coachId && u.TenantId == tenantId, cancellationToken);

        if (!coachExists)
            throw new NotFoundException("Coach", coachId);

        // Validate client exists
        var clientExists = await _context.Users
            .AnyAsync(u => u.Id == request.ClientId && u.TenantId == tenantId, cancellationToken);

        if (!clientExists)
            throw new NotFoundException("Client", request.ClientId);

        // Check no overlapping appointment for coach at same time
        var hasOverlap = await _context.Appointments
            .AnyAsync(a => a.CoachId == coachId
                && a.TenantId == tenantId
                && !a.IsDeleted
                && a.Status != AppointmentStatus.Cancelled
                && a.StartTime < request.EndTime
                && a.EndTime > request.StartTime,
                cancellationToken);

        if (hasOverlap)
            throw new ConflictException("Coach already has an appointment during this time slot.");

        var appointment = new Appointment
        {
            TenantId = tenantId,
            CoachId = coachId,
            ClientId = request.ClientId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Title = request.Title,
            Notes = request.Notes,
            Status = AppointmentStatus.Pending
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync(cancellationToken);

        return appointment.Id;
    }
}
