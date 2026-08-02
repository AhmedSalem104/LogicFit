using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Platform.Auth;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LogicFit.Tests;

public sealed class PlatformOwnerBootstrapTests
{
    [Fact]
    public void Enabled_bootstrap_rejects_incomplete_or_weak_server_configuration()
    {
        var options = ValidOptions();
        options.PhoneNumber = "01000000000";

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PlatformOwnerBootstrapOptions.Validate(options));

        Assert.Contains("E.164", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Disabled_bootstrap_never_creates_a_default_owner()
    {
        await using var fixture = await BootstrapFixture.CreateAsync(new PlatformOwnerBootstrapOptions());

        await fixture.Seeder.SeedAsync();

        Assert.False(await fixture.Db.Set<User>().IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == PlatformConstants.PlatformTenantId && x.Role == UserRole.PlatformOwner));
    }

    [Fact]
    public async Task Enabled_bootstrap_creates_one_login_ready_owner_idempotently()
    {
        var options = ValidOptions();
        await using var fixture = await BootstrapFixture.CreateAsync(options);

        await fixture.Seeder.SeedAsync();
        var firstOwner = await fixture.Db.Set<User>().IgnoreQueryFilters().SingleAsync(x =>
            x.TenantId == PlatformConstants.PlatformTenantId && x.Role == UserRole.PlatformOwner);
        var firstIdentity = await fixture.Db.IdentityAccounts.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == firstOwner.IdentityAccountId);
        var firstPasswordHash = firstOwner.PasswordHash;
        var firstEmailVerifiedAt = firstIdentity.EmailVerifiedAt;
        var firstPhoneVerifiedAt = firstIdentity.PhoneVerifiedAt;

        await fixture.Seeder.SeedAsync();
        fixture.Db.ChangeTracker.Clear();

        var owners = await fixture.Db.Set<User>().IgnoreQueryFilters()
            .Where(x => x.TenantId == PlatformConstants.PlatformTenantId && x.Role == UserRole.PlatformOwner)
            .ToListAsync();
        var owner = Assert.Single(owners);
        var identity = await fixture.Db.IdentityAccounts.IgnoreQueryFilters()
            .SingleAsync(x => x.Id == owner.IdentityAccountId);

        Assert.True(owner.IsActive);
        Assert.False(owner.IsDeleted);
        Assert.True(BCrypt.Net.BCrypt.Verify(options.Password!, owner.PasswordHash));
        Assert.True(identity.IsActive);
        Assert.NotNull(identity.EmailVerifiedAt);
        Assert.NotNull(identity.PhoneVerifiedAt);
        Assert.Equal("OWNER@LOGICFIT.TEST", identity.NormalizedEmail);
        Assert.Equal("+201012345678", identity.NormalizedPhoneNumber);
        Assert.True(BCrypt.Net.BCrypt.Verify(options.Password!, identity.PasswordHash));
        Assert.Equal(firstPasswordHash, owner.PasswordHash);
        Assert.Equal(firstEmailVerifiedAt, identity.EmailVerifiedAt);
        Assert.Equal(firstPhoneVerifiedAt, identity.PhoneVerifiedAt);
    }

    [Fact]
    public async Task Enabled_bootstrap_repairs_legacy_owner_and_revokes_old_sessions()
    {
        var options = ValidOptions();
        await using var fixture = await BootstrapFixture.CreateAsync(options);
        var owner = new User
        {
            TenantId = PlatformConstants.PlatformTenantId,
            Email = "legacy@platform.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Old#Password123"),
            Role = UserRole.PlatformOwner,
            IsActive = true
        };
        fixture.Db.Tenants.Add(new Tenant
        {
            Id = PlatformConstants.PlatformTenantId,
            Name = "Platform",
            Status = TenantStatus.Active
        });
        fixture.Db.Set<User>().Add(owner);
        fixture.Db.RefreshTokens.Add(new RefreshToken
        {
            UserId = owner.Id,
            Surface = "platform",
            Token = "hashed-test-token",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });
        await fixture.Db.SaveChangesAsync();

        await fixture.Seeder.SeedAsync();

        await fixture.Db.Entry(owner).ReloadAsync();
        var identity = await fixture.Db.IdentityAccounts.SingleAsync(x => x.Id == owner.IdentityAccountId);
        var session = await fixture.Db.RefreshTokens.SingleAsync(x => x.UserId == owner.Id);
        Assert.NotNull(owner.IdentityAccountId);
        Assert.Equal(options.Email, owner.Email);
        Assert.Equal(options.PhoneNumber, owner.PhoneNumber);
        Assert.NotNull(identity.EmailVerifiedAt);
        Assert.NotNull(identity.PhoneVerifiedAt);
        Assert.Null(identity.LockoutEndUtc);
        Assert.NotNull(session.RevokedAt);
    }

    [Fact]
    public async Task Seeder_repairs_required_platform_role_when_an_unrelated_assignment_already_exists()
    {
        await using var fixture = await BootstrapFixture.CreateAsync(new PlatformOwnerBootstrapOptions());
        await fixture.Seeder.SeedAsync();
        var owner = await AddLegacyPlatformOwnerWithUnrelatedRoleAsync(fixture.Db);
        var originalVersion = owner.PermissionsVersion;

        await fixture.Seeder.SeedAsync();
        fixture.Db.ChangeTracker.Clear();

        var assignments = await fixture.Db.UserRoleAssignments
            .IgnoreQueryFilters()
            .Where(x => x.UserId == owner.Id)
            .Select(x => new { x.Role.Name, x.TenantId })
            .ToListAsync();
        var repairedOwner = await fixture.Db.Set<User>().IgnoreQueryFilters().SingleAsync(x => x.Id == owner.Id);
        var authorization = await new RbacService(fixture.Db).GetUserAuthorizationAsync(owner.Id);

        Assert.Contains(assignments, x => x.Name == SystemRoles.Owner);
        Assert.Contains(assignments, x => x.Name == SystemRoles.PlatformOwner && x.TenantId == null);
        Assert.Equal(originalVersion + 1, repairedOwner.PermissionsVersion);
        Assert.Contains(SystemRoles.PlatformOwner, authorization.Roles);
        Assert.Contains(Permissions.ManageTenants, authorization.Permissions);

        await fixture.Seeder.SeedAsync();
        fixture.Db.ChangeTracker.Clear();

        Assert.Equal(2, await fixture.Db.UserRoleAssignments.IgnoreQueryFilters().CountAsync(x => x.UserId == owner.Id));
        Assert.Equal(originalVersion + 1,
            await fixture.Db.Set<User>().IgnoreQueryFilters().Where(x => x.Id == owner.Id).Select(x => x.PermissionsVersion).SingleAsync());
    }

    [Fact]
    public async Task Platform_session_repairs_and_signs_the_required_role_before_issuing_a_token()
    {
        await using var fixture = await BootstrapFixture.CreateAsync(new PlatformOwnerBootstrapOptions());
        await fixture.Seeder.SeedAsync();
        var owner = await AddLegacyPlatformOwnerWithUnrelatedRoleAsync(fixture.Db);
        var jwt = new CapturingJwtService();
        var issuer = new PlatformSessionIssuer(
            fixture.Db,
            jwt,
            new RbacService(fixture.Db),
            new FakeRefreshTokenService(),
            new FakeCurrentUserService());

        var response = await issuer.IssueAsync(owner.IdentityAccountId!.Value);

        Assert.Contains(SystemRoles.PlatformOwner, jwt.Roles);
        Assert.Contains(Permissions.ManageTenants, jwt.Permissions);
        Assert.Contains(SystemRoles.PlatformOwner, response.Roles);
        Assert.Contains(Permissions.ManageTenants, response.Permissions);
        Assert.Equal(1, owner.PermissionsVersion);
        Assert.Single(await fixture.Db.UserRoleAssignments.IgnoreQueryFilters()
            .Where(x => x.UserId == owner.Id && x.Role.Name == SystemRoles.PlatformOwner)
            .ToListAsync());

        await issuer.IssueAsync(owner.IdentityAccountId.Value);

        Assert.Equal(1, owner.PermissionsVersion);
        Assert.Single(await fixture.Db.UserRoleAssignments.IgnoreQueryFilters()
            .Where(x => x.UserId == owner.Id && x.Role.Name == SystemRoles.PlatformOwner)
            .ToListAsync());
    }

    [Fact]
    public async Task Seeder_preserves_an_existing_tenant_assignment_instead_of_restoring_a_stale_legacy_role()
    {
        await using var fixture = await BootstrapFixture.CreateAsync(new PlatformOwnerBootstrapOptions());
        await fixture.Seeder.SeedAsync();
        var tenant = new Tenant { Name = "RBAC tenant", Subdomain = $"rbac-{Guid.NewGuid():N}", Status = TenantStatus.Active };
        var user = new User
        {
            TenantId = tenant.Id,
            Email = "tenant-rbac@logicfit.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Strong#Password123"),
            Role = UserRole.Manager,
            IsActive = true
        };
        var explicitOwnerRoleId = await fixture.Db.AppRoles.IgnoreQueryFilters()
            .Where(x => x.Name == SystemRoles.Owner)
            .Select(x => x.Id)
            .SingleAsync();
        fixture.Db.Tenants.Add(tenant);
        fixture.Db.Set<User>().Add(user);
        fixture.Db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = explicitOwnerRoleId,
            TenantId = tenant.Id
        });
        await fixture.Db.SaveChangesAsync();

        await fixture.Seeder.SeedAsync();
        fixture.Db.ChangeTracker.Clear();

        var roles = await fixture.Db.UserRoleAssignments.IgnoreQueryFilters()
            .Where(x => x.UserId == user.Id)
            .Select(x => x.Role.Name)
            .ToListAsync();
        Assert.Equal(new[] { SystemRoles.Owner }, roles);
        Assert.Equal(0,
            await fixture.Db.Set<User>().IgnoreQueryFilters().Where(x => x.Id == user.Id).Select(x => x.PermissionsVersion).SingleAsync());
    }

    private static async Task<User> AddLegacyPlatformOwnerWithUnrelatedRoleAsync(ApplicationDbContext db)
    {
        var identity = new IdentityAccount
        {
            FullName = "Legacy Platform Owner",
            Email = "legacy-owner@logicfit.test",
            NormalizedEmail = "LEGACY-OWNER@LOGICFIT.TEST",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Strong#Password123"),
            EmailVerifiedAt = DateTime.UtcNow,
            IsActive = true
        };
        var owner = new User
        {
            IdentityAccountId = identity.Id,
            TenantId = PlatformConstants.PlatformTenantId,
            Email = identity.Email,
            PasswordHash = identity.PasswordHash,
            Role = UserRole.PlatformOwner,
            IsActive = true
        };
        var unrelatedRoleId = await db.AppRoles.IgnoreQueryFilters()
            .Where(x => x.Name == SystemRoles.Owner)
            .Select(x => x.Id)
            .SingleAsync();

        db.IdentityAccounts.Add(identity);
        db.Set<User>().Add(owner);
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = owner.Id,
            RoleId = unrelatedRoleId,
            TenantId = null
        });
        await db.SaveChangesAsync();
        return owner;
    }

    private static PlatformOwnerBootstrapOptions ValidOptions() => new()
    {
        Enabled = true,
        Email = "owner@logicfit.test",
        Password = "Strong#Password123",
        PhoneNumber = "+201012345678",
        FullName = "LogicFit Platform Owner",
        ResetPassword = true
    };

    private sealed class BootstrapFixture : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; }
        public RbacSeeder Seeder { get; }

        private BootstrapFixture(ApplicationDbContext db, RbacSeeder seeder)
            => (Db, Seeder) = (db, seeder);

        public static async Task<BootstrapFixture> CreateAsync(PlatformOwnerBootstrapOptions options)
        {
            var databaseName = $"LogicFitPlatformBootstrapTests_{Guid.NewGuid():N}";
            var baseConnectionString = Environment.GetEnvironmentVariable("LOGICFIT_TEST_CONNECTION_STRING")
                ?? "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
            var connectionString = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = databaseName
            }.ConnectionString;
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            var db = new ApplicationDbContext(
                dbOptions,
                new FakeTenantService(),
                new FakeCurrentUserService(),
                new SystemClock());
            await db.Database.EnsureCreatedAsync();
            var seeder = new RbacSeeder(
                db,
                NullLogger<RbacSeeder>.Instance,
                Options.Create(options));
            return new BootstrapFixture(db, seeder);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.Database.EnsureDeletedAsync();
            await Db.DisposeAsync();
            SqlConnection.ClearAllPools();
        }
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

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public string? UserId => null;
        public string? UserName => null;
        public Guid? TenantId => null;
        public bool IsAuthenticated => false;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "LogicFit.Tests";
    }

    private sealed class CapturingJwtService : IJwtService
    {
        public IReadOnlyList<string> Roles { get; private set; } = Array.Empty<string>();
        public IReadOnlyList<string> Permissions { get; private set; } = Array.Empty<string>();

        public AccessTokenResult GenerateAccessToken(
            Guid userId,
            string email,
            Guid? tenantId,
            IEnumerable<string> roles,
            IEnumerable<string> permissions,
            int permissionVersion)
        {
            Roles = roles.ToArray();
            Permissions = permissions.ToArray();
            return new AccessTokenResult("test-platform-access-token", DateTime.UtcNow.AddMinutes(15));
        }

        public string GenerateRefreshToken() => "test-refresh-token";
    }

    private sealed class FakeRefreshTokenService : IRefreshTokenService
    {
        public RefreshToken Issue(User user, string? ipAddress, string surface) => new()
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Surface = surface,
            Token = "test-platform-refresh-token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedByIp = ipAddress
        };

        public Task<(User user, RefreshToken newToken)> RotateAsync(
            string token,
            string? ipAddress,
            string expectedSurface,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task RevokeAllAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class SystemClock : IDateTimeService
    {
        public DateTime Now => DateTime.Now;
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
