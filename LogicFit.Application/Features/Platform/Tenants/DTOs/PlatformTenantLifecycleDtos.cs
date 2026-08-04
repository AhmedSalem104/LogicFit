using LogicFit.Domain.Enums;

namespace LogicFit.Application.Features.Platform.Tenants.DTOs;

/// <summary>Safe owner-account metadata. Passwords and password hashes are intentionally absent.</summary>
public sealed record PlatformTenantCredentialsDto(
    Guid TenantId,
    string TenantName,
    string? OwnerEmail,
    bool IdentityLinked,
    bool IdentityActive,
    DateTime? EmailVerifiedAtUtc,
    bool OwnerAccountActive,
    WorkspaceMembershipStatus? MembershipStatus,
    DateTime? LastLoginAtUtc,
    DateTime? LockoutEndUtc,
    bool PasswordResetAvailable);

public sealed record PlatformTenantPasswordResetDto(
    Guid TenantId,
    string? OwnerEmail,
    bool ResetEmailAccepted,
    int ExpiresInMinutes);

public sealed record PlatformTenantDeleteRequest(
    string TenantNameConfirmation,
    bool PreserveGlobalIdentity = true);

public sealed record PlatformTenantPermanentDeleteDto(
    Guid TenantId,
    string TenantName,
    string Status,
    Guid BackupBatchId,
    Guid BackupArtifactId,
    Guid DatabaseResourceId,
    bool GlobalIdentityPreserved);
