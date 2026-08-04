using LogicFit.Application.Features.Platform.Tenants.DTOs;

namespace LogicFit.Application.Common.Interfaces;

public interface IPlatformTenantLifecycleService
{
    Task<PlatformTenantCredentialsDto> GetCredentialsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<PlatformTenantPasswordResetDto> RequestPasswordResetAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<PlatformTenantDto> SoftDeleteAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<PlatformTenantDto> RestoreAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<PlatformTenantPermanentDeleteDto> PermanentlyDeleteAsync(
        Guid tenantId,
        PlatformTenantDeleteRequest request,
        CancellationToken cancellationToken = default);
}
