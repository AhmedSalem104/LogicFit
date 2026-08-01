using LogicFit.Application.Common.Services;

namespace LogicFit.API.Security;

public interface IRefreshTokenCookieManager
{
    string? Read(HttpRequest request, string surface);
    void Write(HttpResponse response, string token, string surface);
    void Delete(HttpResponse response, string surface);
}

public sealed class RefreshTokenCookieManager : IRefreshTokenCookieManager
{
    private const string TenantCookie = "__Host-logicfit-tenant-refresh";
    private const string PlatformCookie = "__Host-logicfit-platform-refresh";

    public string? Read(HttpRequest request, string surface)
        => request.Cookies.TryGetValue(Name(surface), out var value) ? value : null;

    public void Write(HttpResponse response, string token, string surface)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("A refresh token cannot be written when empty.");

        response.Cookies.Append(Name(surface), token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(RefreshTokenService.RefreshTokenExpiryDays)
        });
    }

    public void Delete(HttpResponse response, string surface)
        => response.Cookies.Delete(Name(surface), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            IsEssential = true
        });

    private static string Name(string surface)
        => surface == RefreshTokenService.SurfacePlatform ? PlatformCookie : TenantCookie;
}
