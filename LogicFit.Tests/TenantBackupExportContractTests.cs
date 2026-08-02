using LogicFit.API.Features.TenantBackups;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LogicFit.Tests;

public sealed class TenantBackupExportContractTests
{
    [Fact]
    public void Tenant_permission_is_not_a_platform_permission()
    {
        Assert.Contains(Permissions.CreateAndDownloadTenantBackup, Permissions.TenantPermissions);
        Assert.DoesNotContain(Permissions.CreateAndDownloadTenantBackup, Permissions.PlatformPermissions);
    }

    [Fact]
    public void Download_grant_contract_does_not_expose_database_metadata()
    {
        var properties = typeof(TenantBackupDownloadGrantDto).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain(properties, x => x.Contains("Connection", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, x => x.Contains("Database", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nameof(TenantBackupDownloadGrantDto.DownloadToken), properties);
    }

    [Fact]
    public async Task Controller_derives_tenant_from_server_context_and_forwards_only_grant()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = new StubExportService();
        var controller = new TenantBackupsController(service, new StubTenantService(tenantId), new StubCurrentUser(userId));

        var response = await controller.Create(
            new TenantBackupExportRequest("grant-token", "request-1"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal("grant-token", service.LastRequest?.GrantToken);
        Assert.Equal(tenantId, service.LastTenantId);
        Assert.Equal(userId, service.LastUserId);
        Assert.IsType<TenantBackupExportDto>(ok.Value);
    }

    private sealed class StubExportService : ITenantBackupExportService
    {
        public Guid LastTenantId { get; private set; }
        public Guid LastUserId { get; private set; }
        public TenantBackupExportRequest? LastRequest { get; private set; }

        public Task<SensitiveActionGrantDto> ReauthenticateAsync(Guid userId, Guid tenantId, string currentPassword, CancellationToken cancellationToken = default)
            => Task.FromResult(new SensitiveActionGrantDto("grant", DateTime.UtcNow.AddMinutes(5), SensitiveActionScopes.TenantBackupExport));

        public Task<SensitiveActionGrantDto> ReauthenticateForDownloadAsync(Guid userId, Guid tenantId, string currentPassword, CancellationToken cancellationToken = default)
            => Task.FromResult(new SensitiveActionGrantDto("download-grant", DateTime.UtcNow.AddMinutes(5), SensitiveActionScopes.TenantBackupDownload));

        public Task<TenantBackupExportDto> CreateAsync(Guid userId, Guid tenantId, TenantBackupExportRequest request, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            LastTenantId = tenantId;
            LastRequest = request;
            return Task.FromResult(new TenantBackupExportDto(Guid.NewGuid(), TenantBackupExportStatus.Completed, DateTime.UtcNow, null, DateTime.UtcNow, null, 10, "hash", null));
        }

        public Task<IReadOnlyList<TenantBackupExportDto>> ListAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TenantBackupExportDto>>([]);

        public Task<TenantBackupExportDto> GetAsync(Guid userId, Guid tenantId, Guid exportId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantBackupDownloadGrantDto> CreateDownloadGrantAsync(Guid userId, Guid tenantId, Guid exportId, string grantToken, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantBackupDownload> OpenDownloadAsync(Guid userId, Guid tenantId, Guid exportId, string downloadToken, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubTenantService(Guid tenantId) : ITenantService
    {
        private readonly Guid _tenantId = tenantId;
        public Guid? CurrentTenantId => _tenantId;
        public Guid GetCurrentTenantId() => _tenantId;
        public Task SetTenantAsync(Guid tenantId) => Task.CompletedTask;
        public Task SetTenantBySubdomainAsync(string subdomain) => Task.CompletedTask;
        public Task<bool> SetTenantByCustomDomainAsync(string host) => Task.FromResult(false);
        public Task<bool> TenantExistsAsync(Guid tenantId) => Task.FromResult(tenantId == _tenantId);
        public Task<Guid?> ResolveTenantIdAsync(string identifier) => Task.FromResult<Guid?>(_tenantId);
    }

    private sealed class StubCurrentUser(Guid userId) : ICurrentUserService
    {
        public string? UserId => userId.ToString();
        public string? UserName => null;
        public Guid? TenantId => null;
        public bool IsAuthenticated => true;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "test";
    }
}
