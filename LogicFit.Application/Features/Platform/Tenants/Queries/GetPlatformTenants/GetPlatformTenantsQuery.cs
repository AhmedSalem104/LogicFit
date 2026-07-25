using LogicFit.Application.Common.Models;
using LogicFit.Application.Features.Platform.Tenants.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Platform.Tenants.Queries.GetPlatformTenants;

public class GetPlatformTenantsQuery : IRequest<PagedResult<PlatformTenantDto>>
{
    public TenantStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PageRequest.DefaultPageSize;
}
