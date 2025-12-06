using LogicFit.Application.Features.Tenants.DTOs;
using MediatR;

namespace LogicFit.Application.Features.Tenants.Commands.CreateTenant;

public record CreateTenantCommand(
    string Name,
    string Subdomain,
    string? LogoUrl = null,
    string? PrimaryColor = null,
    string? SecondaryColor = null
) : IRequest<TenantDto>;
