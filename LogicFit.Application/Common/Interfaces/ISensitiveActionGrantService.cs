namespace LogicFit.Application.Common.Interfaces;

public static class SensitiveActionScopes
{
    public const string TenantBackupExport = "tenant-backup-export";
    public const string TenantBackupDownload = "tenant-backup-download";
}

public sealed record SensitiveActionGrantDto(string GrantToken, DateTime ExpiresAtUtc, string Scope);

public interface ISensitiveActionGrantService
{
    Task<SensitiveActionGrantDto> ReauthenticateAsync(
        Guid userId,
        Guid? tenantId,
        string currentPassword,
        string scope,
        CancellationToken cancellationToken = default);

    Task ConsumeAsync(
        string rawGrantToken,
        Guid userId,
        Guid? tenantId,
        string scope,
        CancellationToken cancellationToken = default);
}
