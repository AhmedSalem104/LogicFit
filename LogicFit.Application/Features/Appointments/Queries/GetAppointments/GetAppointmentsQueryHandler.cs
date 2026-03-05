using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Appointments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Appointments.Queries.GetAppointments;

public class GetAppointmentsQueryHandler : IRequestHandler<GetAppointmentsQuery, List<AppointmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetAppointmentsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<AppointmentDto>> Handle(GetAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.Appointments
            .Include(a => a.Coach).ThenInclude(c => c.Profile)
            .Include(a => a.Client).ThenInclude(c => c.Profile)
            .Where(a => a.TenantId == tenantId && !a.IsDeleted)
            .AsQueryable();

        if (request.CoachId.HasValue)
            query = query.Where(a => a.CoachId == request.CoachId.Value);

        if (request.ClientId.HasValue)
            query = query.Where(a => a.ClientId == request.ClientId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(a => a.StartTime >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.EndTime <= request.ToDate.Value);

        if (request.Status.HasValue)
            query = query.Where(a => a.Status == request.Status.Value);

        var appointments = await query
            .OrderBy(a => a.StartTime)
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
            .ToListAsync(cancellationToken);

        return appointments;
    }
}
