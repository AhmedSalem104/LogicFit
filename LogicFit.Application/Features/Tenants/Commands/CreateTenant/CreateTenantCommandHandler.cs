using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Tenants.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.ValueObjects;
using MediatR;

namespace LogicFit.Application.Features.Tenants.Commands.CreateTenant;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantDto>
{
    private readonly IApplicationDbContext _context;

    public CreateTenantCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = new Tenant
        {
            Name = request.Name,
            Subdomain = request.Subdomain.ToLower(),
            Status = SubscriptionStatus.Active,
            BrandingSettings = new BrandingSettings
            {
                LogoUrl = request.LogoUrl,
                PrimaryColor = request.PrimaryColor ?? "#3B82F6",
                SecondaryColor = request.SecondaryColor ?? "#1E40AF"
            }
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            Status = tenant.Status,
            CreatedAt = tenant.CreatedAt
        };
    }
}
