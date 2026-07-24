using LogicFit.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Platform.Subscriptions.Queries.GetTenantUsage;

public sealed class GetTenantUsageQuery : IRequest<List<TenantUsageDto>> { }
public sealed class TenantUsageDto
{
    public Guid TenantId { get; init; }
    public int MembersCount { get; init; }
    public int CoachesCount { get; init; }
    public int EmployeesCount { get; init; }
    public int BranchesCount { get; init; }
    public int StorageUsedMB { get; init; }
    public DateTime LastCalculatedAt { get; init; }
}
public sealed class GetTenantUsageQueryHandler(IApplicationDbContext context) : IRequestHandler<GetTenantUsageQuery, List<TenantUsageDto>>
{
    public Task<List<TenantUsageDto>> Handle(GetTenantUsageQuery request, CancellationToken cancellationToken)
        => context.TenantUsages.AsNoTracking().OrderByDescending(x => x.LastCalculatedAt).Select(x => new TenantUsageDto
        { TenantId = x.TenantId, MembersCount = x.MembersCount, CoachesCount = x.CoachesCount, EmployeesCount = x.EmployeesCount, BranchesCount = x.BranchesCount, StorageUsedMB = x.StorageUsedMB, LastCalculatedAt = x.LastCalculatedAt }).ToListAsync(cancellationToken);
}
