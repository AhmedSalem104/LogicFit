using System.Reflection;
using LogicFit.API.Security;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Services;
using LogicFit.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Xunit;

namespace LogicFit.Tests;

public sealed class AuthSecurityTests
{
    [Fact]
    public async Task Refresh_rotation_detects_reuse_and_revokes_the_token_family()
    {
        await using var fixture = await RefreshFixture.CreateAsync();
        var tenant = new Tenant
        {
            Name = "Refresh Test Workspace",
            Subdomain = $"refresh-{Guid.NewGuid():N}",
            Status = TenantStatus.Active
        };
        var user = new User
        {
            TenantId = tenant.Id,
            Email = $"refresh-{Guid.NewGuid():N}@logicfit.test",
            PasswordHash = "not-used-by-this-test",
            Role = UserRole.Owner,
            IsActive = true
        };
        fixture.Db.Tenants.Add(tenant);
        fixture.Db.Set<User>().Add(user);

        var service = new RefreshTokenService(fixture.Db, new FakeJwtService(), fixture.Clock);
        var original = service.Issue(user, "127.0.0.1", RefreshTokenService.SurfaceTenant);
        await fixture.Db.SaveChangesAsync();
        var originalValue = original.Token;

        var (_, replacement) = await service.RotateAsync(
            originalValue, "127.0.0.2", RefreshTokenService.SurfaceTenant);
        var replacementValue = replacement.Token;

        var error = await Assert.ThrowsAsync<UnauthorizedException>(() => service.RotateAsync(
            originalValue, "127.0.0.3", RefreshTokenService.SurfaceTenant));
        Assert.Equal("REFRESH_TOKEN_REUSE_DETECTED", error.Message);

        fixture.Db.ChangeTracker.Clear();
        var storedOriginal = await fixture.Db.RefreshTokens.SingleAsync(x => x.Token == originalValue);
        var storedReplacement = await fixture.Db.RefreshTokens.SingleAsync(x => x.Token == replacementValue);
        Assert.NotNull(storedOriginal.RevokedAt);
        Assert.NotNull(storedReplacement.RevokedAt);
    }

    [Fact]
    public void Refresh_token_is_json_hidden_and_cookie_is_hardened()
    {
        var property = typeof(AuthResponseDto).GetProperty(nameof(AuthResponseDto.RefreshToken))!;
        Assert.NotNull(property.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>());
        var context = new DefaultHttpContext();
        new RefreshTokenCookieManager().Write(context.Response, "opaque-token", RefreshTokenService.SurfaceTenant);
        var header = Assert.Single(context.Response.Headers["Set-Cookie"].ToArray()) ?? string.Empty;
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", header, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("opaque-token", System.Text.Json.JsonSerializer.Serialize(
            new AuthResponseDto { RefreshToken = "opaque-token" }));
    }

    [Fact]
    public void Runtime_routes_do_not_expose_passkey_webauthn_or_otp_step_up()
    {
        var assembly = typeof(RefreshTokenCookieManager).Assembly;
        var runtimeTypes = assembly.GetTypes()
            .Where(x => x.Namespace?.Contains(".Persistence.Migrations", StringComparison.Ordinal) != true)
            .Select(x => x.FullName ?? x.Name)
            .ToArray();

        Assert.DoesNotContain(runtimeTypes, x =>
            x.Contains("Passkey", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("WebAuthn", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("OtpStepUp", StringComparison.OrdinalIgnoreCase));

        var routes = assembly.GetTypes()
            .Where(x => typeof(ControllerBase).IsAssignableFrom(x))
            .SelectMany(x => x.GetMethods())
            .SelectMany(x => x.GetCustomAttributes<HttpMethodAttribute>())
            .Select(x => x.Template ?? string.Empty);
        Assert.DoesNotContain(routes, x =>
            x.Contains("passkey", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("step-up", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("phone-login", StringComparison.OrdinalIgnoreCase));

        var authorizationPolicies = assembly.GetTypes()
            .Where(x => typeof(ControllerBase).IsAssignableFrom(x))
            .SelectMany(x => x.GetCustomAttributes<AuthorizeAttribute>()
                .Concat(x.GetMethods().SelectMany(method => method.GetCustomAttributes<AuthorizeAttribute>())))
            .Select(x => x.Policy ?? string.Empty);
        Assert.DoesNotContain(authorizationPolicies,
            x => x.Contains("OtpStepUp", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class RefreshFixture : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; }
        public MutableClock Clock { get; }
        private string ConnectionString { get; }

        private RefreshFixture(ApplicationDbContext db, MutableClock clock, string connectionString) =>
            (Db, Clock, ConnectionString) = (db, clock, connectionString);

        public static async Task<RefreshFixture> CreateAsync()
        {
            var databaseName = $"LogicFitAuthTests_{Guid.NewGuid():N}";
            var clock = new MutableClock();
            var baseConnectionString = Environment.GetEnvironmentVariable("LOGICFIT_TEST_CONNECTION_STRING")
                ?? "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
            var connectionString = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = databaseName
            }.ConnectionString;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            var db = new ApplicationDbContext(options, new FakeTenantService(), new FakeCurrentUser(), clock);
            await db.Database.EnsureCreatedAsync();
            return new RefreshFixture(db, clock, connectionString);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.Database.EnsureDeletedAsync();
            await Db.DisposeAsync();
        }
    }

    private sealed class MutableClock : IDateTimeService
    {
        private DateTime _utcNow = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        public DateTime Now => _utcNow.ToLocalTime();
        public DateTime UtcNow => _utcNow;
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public string? UserId => null;
        public string? UserName => null;
        public Guid? TenantId => null;
        public bool IsAuthenticated => false;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "LogicFit.Tests";
    }

    private sealed class FakeTenantService : ITenantService
    {
        public Guid? CurrentTenantId => null;
        public Task SetTenantAsync(Guid tenantId) => Task.CompletedTask;
        public Task SetTenantBySubdomainAsync(string subdomain) => Task.CompletedTask;
        public Task<bool> SetTenantByCustomDomainAsync(string host) => Task.FromResult(false);
        public Task<bool> TenantExistsAsync(Guid tenantId) => Task.FromResult(false);
        public Task<Guid?> ResolveTenantIdAsync(string identifier) => Task.FromResult<Guid?>(null);
    }

    private sealed class FakeJwtService : IJwtService
    {
        public AccessTokenResult GenerateAccessToken(
            Guid userId, string email, Guid? tenantId, IEnumerable<string> roles,
            IEnumerable<string> permissions, int permissionVersion) =>
            new($"access-{Guid.NewGuid():N}", DateTime.UtcNow.AddMinutes(5));

        public string GenerateRefreshToken() => $"refresh-{Guid.NewGuid():N}";
    }
}
