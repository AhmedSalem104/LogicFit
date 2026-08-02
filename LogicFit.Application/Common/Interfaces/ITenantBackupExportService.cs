using LogicFit.Domain.Enums;

namespace LogicFit.Application.Common.Interfaces;

public sealed record TenantBackupExportRequest(string GrantToken, string? IdempotencyKey = null);

public sealed record TenantBackupExportDto(
    Guid Id,
    TenantBackupExportStatus Status,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? DownloadedAtUtc,
    long? SizeBytes,
    string? Sha256,
    string? ErrorCode);

public sealed record TenantBackupDownloadGrantDto(
    Guid ExportId,
    string DownloadToken,
    DateTime ExpiresAtUtc,
    string DownloadPath);

public sealed record TenantBackupDownload(string FileName, long SizeBytes, Stream Content);

public interface ITenantBackupExportService
{
    Task<SensitiveActionGrantDto> ReauthenticateAsync(
        Guid userId,
        Guid tenantId,
        string currentPassword,
        CancellationToken cancellationToken = default);

    Task<SensitiveActionGrantDto> ReauthenticateForDownloadAsync(
        Guid userId,
        Guid tenantId,
        string currentPassword,
        CancellationToken cancellationToken = default);

    Task<TenantBackupExportDto> CreateAsync(
        Guid userId,
        Guid tenantId,
        TenantBackupExportRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantBackupExportDto>> ListAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<TenantBackupExportDto> GetAsync(
        Guid userId,
        Guid tenantId,
        Guid exportId,
        CancellationToken cancellationToken = default);

    Task<TenantBackupDownloadGrantDto> CreateDownloadGrantAsync(
        Guid userId,
        Guid tenantId,
        Guid exportId,
        string grantToken,
        CancellationToken cancellationToken = default);

    Task<TenantBackupDownload> OpenDownloadAsync(
        Guid userId,
        Guid tenantId,
        Guid exportId,
        string downloadToken,
        CancellationToken cancellationToken = default);
}
