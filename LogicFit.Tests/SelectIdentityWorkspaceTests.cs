using System.Security.Cryptography;
using System.Text;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.Identity.Commands.SelectIdentityWorkspace;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class SelectIdentityWorkspaceTests
{
    [Fact]
    public async Task Active_selection_issues_tenant_session_after_loading_local_account()
    {
        await using var fixture = await ApplicationFixture.CreateAsync();
        var data = await fixture.SeedActiveWorkspaceAsync();
        var scope = new FakeWorkspaceDatabaseScope(true);
        var handler = CreateHandler(fixture.Db, scope);

        var response = await handler.Handle(
            new SelectIdentityWorkspaceCommand(data.RawSessionToken, data.Tenant.Id),
            CancellationToken.None);

        Assert.Equal(data.User.Id, response.UserId);
        Assert.Equal(data.Tenant.Id, response.TenantId);
        Assert.Equal(data.User.Email, response.Email);
        Assert.Equal("tenant-access-token", response.AccessToken);
        Assert.Equal("tenant-refresh-token", response.RefreshToken);
        Assert.Equal(1, scope.OpenCalls);
        Assert.Equal(1, scope.CloseCalls);
    }

    [Fact]
    public async Task Selection_returns_typed_unavailable_error_when_mapping_is_missing()
    {
        await using var fixture = await ApplicationFixture.CreateAsync();
        var data = await fixture.SeedActiveWorkspaceAsync();
        var scope = new FakeWorkspaceDatabaseScope(false);
        var handler = CreateHandler(fixture.Db, scope);

        var exception = await Assert.ThrowsAsync<TenantAccessException>(() => handler.Handle(
            new SelectIdentityWorkspaceCommand(data.RawSessionToken, data.Tenant.Id),
            CancellationToken.None));

        Assert.Equal("TENANT_DATABASE_UNAVAILABLE", exception.Code);
        Assert.Equal(503, exception.StatusCode);
        Assert.Equal(1, scope.OpenCalls);
        Assert.Equal(0, scope.CloseCalls);
    }

    [Fact]
    public async Task Selection_returns_typed_account_error_and_closes_scope_when_local_user_is_missing()
    {
        await using var fixture = await ApplicationFixture.CreateAsync();
        var data = await fixture.SeedActiveWorkspaceAsync(includeUser: false);
        var scope = new FakeWorkspaceDatabaseScope(true);
        var handler = CreateHandler(fixture.Db, scope);

        var exception = await Assert.ThrowsAsync<TenantAccessException>(() => handler.Handle(
            new SelectIdentityWorkspaceCommand(data.RawSessionToken, data.Tenant.Id),
            CancellationToken.None));

        Assert.Equal("WORKSPACE_ACCOUNT_NOT_FOUND", exception.Code);
        Assert.Equal(403, exception.StatusCode);
        Assert.Equal(1, scope.CloseCalls);
    }

    [Fact]
    public void Platform_boundary_selection_query_does_not_translate_tenant_user_navigation()
    {
        using var context = new PlatformDbContext(new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=LogicFitSelectWorkspaceModel;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options);
        var query = context.WorkspaceMemberships
            .IgnoreQueryFilters()
            .Where(x => x.IdentityAccountId != Guid.Empty &&
                        x.TenantId != Guid.Empty &&
                        x.Status == WorkspaceMembershipStatus.Active &&
                        !x.IsDeleted)
            .Select(x => new { x.TenantId, x.UserId, x.Role });

        var sql = query.ToQueryString();

        Assert.DoesNotContain("DomainUsers", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UserProfiles", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WorkspaceMemberships", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static SelectIdentityWorkspaceCommandHandler CreateHandler(
        IApplicationDbContext context,
        FakeWorkspaceDatabaseScope scope,
        IdentityWorkspaceAccessDecision? identityDecision = null)
        => new(
            context,
            new FixedClock(),
            new FakeJwtService(),
            new FakeRefreshTokenService(),
            new FakeRbacService(),
            new FakeCurrentUserService(),
            new FakeTenantAccessGuard(),
            new FakeIdentityWorkspaceAccessGuard(identityDecision ?? new IdentityWorkspaceAccessDecision(IdentityWorkspaceAccessMode.Allowed)),
            scope);

    private sealed class ApplicationFixture : IAsyncDisposable
    {
        private readonly string _connectionString;
        public ApplicationDbContext Db { get; }
        public FixedClock Clock { get; } = new();

        private ApplicationFixture(ApplicationDbContext db, string connectionString)
            => (Db, _connectionString) = (db, connectionString);

        public static async Task<ApplicationFixture> CreateAsync()
        {
            var databaseName = $"LogicFitSelectWorkspaceTests_{Guid.NewGuid():N}";
            var baseConnectionString = Environment.GetEnvironmentVariable("LOGICFIT_TEST_CONNECTION_STRING")
                ?? "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
            var connectionString = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = databaseName
            }.ConnectionString;
            var db = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(connectionString)
                    .Options,
                new FakeTenantService(),
                new FakeCurrentUserService(),
                new FixedClock());
            await db.Database.EnsureCreatedAsync();
            return new ApplicationFixture(db, connectionString);
        }

        public async Task<WorkspaceData> SeedActiveWorkspaceAsync(bool includeUser = true)
        {
            var tenant = new Tenant
            {
                Name = "Selection Gym",
                Subdomain = $"selection-{Guid.NewGuid():N}",
                WorkspaceType = WorkspaceType.Gym,
                Status = TenantStatus.Active
            };
            var identity = new IdentityAccount
            {
                FullName = "Selection Owner",
                Email = $"selection-{Guid.NewGuid():N}@logicfit.test",
                NormalizedEmail = "SELECTION@LOGICFIT.TEST",
                PasswordHash = "test-hash",
                EmailVerifiedAt = DateTime.UtcNow
            };
            var user = new User
            {
                TenantId = tenant.Id,
                IdentityAccountId = identity.Id,
                Email = identity.Email,
                PasswordHash = identity.PasswordHash,
                Role = UserRole.Owner,
                IsActive = true
            };
            var rawSessionToken = CreateRawSessionToken();
            var session = new IdentityWorkspaceSession
            {
                IdentityAccountId = identity.Id,
                TokenHash = HashSessionToken(rawSessionToken),
                ExpiresAt = Clock.UtcNow.AddMinutes(5)
            };
            var membership = new WorkspaceMembership
            {
                TenantId = tenant.Id,
                IdentityAccountId = identity.Id,
                UserId = user.Id,
                Role = UserRole.Owner,
                Status = WorkspaceMembershipStatus.Active
            };

            Db.Tenants.Add(tenant);
            Db.IdentityAccounts.Add(identity);
            if (includeUser)
                Db.UserProfiles.Add(new UserProfile { UserId = user.Id, FullName = "Selection Owner" });
            user.IsDeleted = !includeUser;
            Db.Set<User>().Add(user);
            Db.IdentityWorkspaceSessions.Add(session);
            Db.WorkspaceMemberships.Add(membership);
            await Db.SaveChangesAsync();
            Db.ChangeTracker.Clear();

            return new WorkspaceData(tenant, user, rawSessionToken);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.Database.EnsureDeletedAsync();
            await Db.DisposeAsync();
            SqlConnection.ClearAllPools();
        }
    }

    private sealed record WorkspaceData(Tenant Tenant, User User, string RawSessionToken);

    private static string CreateRawSessionToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static string HashSessionToken(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed class FakeWorkspaceDatabaseScope(bool canOpen) : IWorkspaceDatabaseScope
    {
        public int OpenCalls { get; private set; }
        public int CloseCalls { get; private set; }

        public Task<bool> TryOpenAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            OpenCalls++;
            return Task.FromResult(canOpen);
        }

        public void Close() => CloseCalls++;
    }

    private sealed class FakeIdentityWorkspaceAccessGuard(IdentityWorkspaceAccessDecision decision)
        : IIdentityWorkspaceAccessGuard
    {
        public Task<IdentityWorkspaceAccessDecision> EvaluateAsync(
            Guid userId,
            Guid workspaceId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(decision);
    }

    private sealed class FakeTenantAccessGuard : ITenantAccessGuard
    {
        public Task<TenantAccessState> GetStateAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(new TenantAccessState(
                true,
                TenantStatus.Active,
                TenantSubscriptionStatus.Active,
                null,
                WorkspaceType.Gym));
    }

    private sealed class FakeRbacService : IRbacService
    {
        public Task<UserAuthorization> GetUserAuthorizationAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new UserAuthorization(new[] { "Owner" }, new[] { "ViewMembers" }));

        public Task EnsureUserInRoleAsync(Guid userId, Guid? tenantId, string systemRoleName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeJwtService : IJwtService
    {
        public AccessTokenResult GenerateAccessToken(
            Guid userId,
            string email,
            Guid? tenantId,
            IEnumerable<string> roles,
            IEnumerable<string> permissions,
            int permissionVersion)
            => new("tenant-access-token", DateTime.UtcNow.AddHours(1));

        public string GenerateRefreshToken() => "generated-refresh-token";
    }

    private sealed class FakeRefreshTokenService : IRefreshTokenService
    {
        public RefreshToken Issue(User user, string? ipAddress, string surface)
            => new() { UserId = user.Id, TenantId = user.TenantId, Token = "tenant-refresh-token" };

        public Task<(User user, RefreshToken newToken)> RotateAsync(
            string token,
            string? ipAddress,
            string expectedSurface,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task RevokeAllAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
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
        public Task<bool> TenantExistsAsync(Guid tenantId) => Task.FromResult(true);
        public Task<Guid?> ResolveTenantIdAsync(string identifier) => Task.FromResult<Guid?>(null);
    }

    private sealed class FixedClock : IDateTimeService
    {
        public DateTime Now => UtcNow.ToLocalTime();
        public DateTime UtcNow { get; } = new(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);
    }
}
