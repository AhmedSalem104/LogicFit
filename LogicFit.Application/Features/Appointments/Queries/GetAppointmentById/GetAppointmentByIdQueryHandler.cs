using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Appointments.DTOs;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Appointments.Queries.GetAppointmentById;

public class GetAppointmentByIdQueryHandler : IRequestHandler<GetAppointmentByIdQuery, AppointmentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetAppointmentByIdQueryHandler(IApplicationDbContext context, ITenantService tenantService, ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<AppointmentDto> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var currentUserId = Guid.Parse(_currentUserService.UserId!);
        var role = await _context.Users.Where(u => u.Id == currentUserId && u.TenantId == tenantId)
            .Select(u => u.Role).FirstOrDefaultAsync(cancellationToken);

        var appointment = await _context.Appointments
            .Include(a => a.Coach).ThenInclude(c => c.Profile)
            .Include(a => a.Client).ThenInclude(c => c.Profile)
            .Where(a => a.Id == request.Id && a.TenantId == tenantId && !a.IsDeleted)
            .Where(a => role != UserRole.Client || a.ClientId == currentUserId)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                CoachId = a.CoachId,
                CoachName = a.Coach.Profile != null ? a.Coach.Profile.FullName ?? a.Coach.Email : a.Coach.Email,
                ClientId = a.ClientId,
                ClientName = a.Client.Profile != null ? a.Client.Profile.FullName ?? a.Client.Email : a.Client.Email,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Title = a.Title,
                Notes = a.Notes,
                Status = a.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (appointment == null)
            throw new NotFoundException("Appointment", request.Id);

        return appointment;
    }
}
