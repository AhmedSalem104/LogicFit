using LogicFit.Application.Features.Tenants.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Tenants.Queries.GetTenants;

public record GetTenantsQuery : IRequest<List<TenantDto>>;
