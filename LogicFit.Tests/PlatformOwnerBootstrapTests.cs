using LogicFit.Application.Common.Interfaces;
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

    private sealed class SystemClock : IDateTimeService
    {
        public DateTime Now => DateTime.Now;
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
