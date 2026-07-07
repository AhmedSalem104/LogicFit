using LogicFit.Application.Features.Platform.Tenants.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Platform.Tenants.Queries.GetPlatformTenants;

public class GetPlatformTenantsQuery : IRequest<List<PlatformTenantDto>>
{
    public TenantStatus? Status { get; set; }
}
