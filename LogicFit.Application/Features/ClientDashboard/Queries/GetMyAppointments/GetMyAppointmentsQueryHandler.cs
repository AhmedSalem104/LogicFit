using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClientDashboard.Queries.GetMyAppointments;

public class GetMyAppointmentsQueryHandler : IRequestHandler<GetMyAppointmentsQuery, List<MyAppointmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public GetMyAppointmentsQueryHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<List<MyAppointmentDto>> Handle(GetMyAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var userId = Guid.Parse(_currentUserService.UserId!);

        return await _context.Appointments
            .Include(a => a.Coach).ThenInclude(c => c.Profile)
            .Where(a => a.TenantId == tenantId && a.ClientId == userId)
            .OrderByDescending(a => a.StartTime)
            .Select(a => new MyAppointmentDto
            {
                Id = a.Id,
                CoachName = a.Coach.Profile != null ? a.Coach.Profile.FullName : a.Coach.Email,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Title = a.Title,
                Status = a.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
