using LogicFit.Application.Features.Platform.Tenants.DTOs;
using LogicFit.Domain.Enums;
using MediatR;

namespace LogicFit.Application.Features.Platform.Tenants.Commands.SetTenantStatus;

/// <summary>Platform action to move a tenant to a new lifecycle status (approve/suspend/activate/archive).</summary>
public class SetTenantStatusCommand : IRequest<PlatformTenantDto>
{
    public Guid TenantId { get; set; }
    public TenantStatus Status { get; set; }
}
