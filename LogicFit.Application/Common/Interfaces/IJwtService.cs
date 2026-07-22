namespace LogicFit.Application.Common.Interfaces;

public interface IJwtService
{
    /// <summary>
    /// Issues an access token embedding role names (ClaimTypes.Role), permission codes
    /// ("permission" claims), an optional tenant id, and the permission version ("perm_ver").
    /// tenantId is null for platform users.
    /// </summary>
    AccessTokenResult GenerateAccessToken(
        Guid userId,
        string email,
        Guid? tenantId,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        int permissionVersion);

    string GenerateRefreshToken();
}

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);
