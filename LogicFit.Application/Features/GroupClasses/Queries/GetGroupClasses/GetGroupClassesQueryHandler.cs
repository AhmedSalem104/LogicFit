using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.GroupClasses.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.GroupClasses.Queries.GetGroupClasses;

public class GetGroupClassesQueryHandler : IRequestHandler<GetGroupClassesQuery, List<GroupClassDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public GetGroupClassesQueryHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<List<GroupClassDto>> Handle(GetGroupClassesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;

        var query = _context.GroupClasses
            .Include(g => g.Branch)
            .Include(g => g.Schedules)
            .Where(g => g.TenantId == tenantId)
            .AsQueryable();

        if (request.BranchId.HasValue)
            query = query.Where(g => g.BranchId == request.BranchId.Value);
        if (request.IsActive.HasValue)
            query = query.Where(g => g.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(g => g.Category == request.Category);

        var classes = await query.OrderBy(g => g.Name).ToListAsync(cancellationToken);

        return classes.Select(g => new GroupClassDto
        {
            Id = g.Id,
            TenantId = g.TenantId,
            BranchId = g.BranchId,
            BranchName = g.Branch?.Name,
            Name = g.Name,
            Description = g.Description,
            Category = g.Category,
            DurationMinutes = g.DurationMinutes,
            Capacity = g.Capacity,
            Color = g.Color,
            ImageUrl = g.ImageUrl,
            Price = g.Price,
            IsActive = g.IsActive,
            UpcomingSchedulesCount = g.Schedules.Count(s => s.StartTime >= now && !s.IsCancelled && !s.IsDeleted)
        }).ToList();
    }
}
