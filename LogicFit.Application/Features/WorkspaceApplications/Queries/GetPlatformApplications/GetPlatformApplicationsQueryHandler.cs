using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Models;
using LogicFit.Application.Features.WorkspaceApplications.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.WorkspaceApplications.Queries.GetPlatformApplications;

public sealed class GetPlatformApplicationsQueryHandler
    : IRequestHandler<GetPlatformApplicationsQuery, PagedResult<PlatformApplicationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPlatformApplicationsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PagedResult<PlatformApplicationDto>> Handle(GetPlatformApplicationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ApplicationRequests.Include(x => x.IdentityAccount).AsQueryable();
        if (request.ApplicationType.HasValue)
            query = query.Where(x => x.ApplicationType == request.ApplicationType.Value);
        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        var (page, pageSize) = PageRequest.Normalize(request.Page, request.PageSize);
        var totalCount = await query.CountAsync(cancellationToken);
        var applications = await query
            .OrderByDescending(x => x.SubmittedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = applications
            .Select(x => PlatformApplicationMapper.ToDto(x, x.IdentityAccount.Email, x.IdentityAccount.PhoneNumber))
            .ToList();
        return PagedResult<PlatformApplicationDto>.Create(items, totalCount, page, pageSize);
    }
}
