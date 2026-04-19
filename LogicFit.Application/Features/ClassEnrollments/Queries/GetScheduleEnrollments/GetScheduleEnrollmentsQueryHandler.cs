using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.GroupClasses.DTOs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.ClassEnrollments.Queries.GetScheduleEnrollments;

public class GetScheduleEnrollmentsQueryHandler : IRequestHandler<GetScheduleEnrollmentsQuery, List<ClassEnrollmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GetScheduleEnrollmentsQueryHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<List<ClassEnrollmentDto>> Handle(GetScheduleEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();

        var query = _context.ClassEnrollments
            .Include(e => e.Client)
            .Where(e => e.ScheduleId == request.ScheduleId && e.TenantId == tenantId)
            .AsQueryable();

        if (!request.IncludeCancelled)
            query = query.Where(e => e.Status != ClassEnrollmentStatus.Cancelled);

        var enrollments = await query
            .OrderBy(e => e.Status == ClassEnrollmentStatus.Booked ? 0 : e.Status == ClassEnrollmentStatus.Waitlist ? 1 : 2)
            .ThenBy(e => e.WaitlistPosition)
            .ThenBy(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);

        return enrollments.Select(e => new ClassEnrollmentDto
        {
            Id = e.Id,
            ScheduleId = e.ScheduleId,
            ClientId = e.ClientId,
            ClientName = e.Client.Email,
            EnrolledAt = e.EnrolledAt,
            Status = e.Status,
            WaitlistPosition = e.WaitlistPosition,
            CancelledAt = e.CancelledAt,
            AttendedAt = e.AttendedAt
        }).ToList();
    }
}
