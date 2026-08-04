using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class IdentityWorkspaceSessionIssuerTests
{
    [Fact]
    public async Task Identity_login_repairs_an_active_gyms_pending_owner_membership()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var tenant = new Tenant
        {
            Name = "Air Gym",
            Subdomain = $"air-gym-{Guid.NewGuid():N}",
            WorkspaceType = WorkspaceType.Gym,
            Status = TenantStatus.Active
        };
        var identity = new IdentityAccount
        {
            FullName = "Air Gym Owner",
            Email = $"air-owner-{Guid.NewGuid():N}@logicfit.test",
            NormalizedEmail = $"AIR-OWNER-{Guid.NewGuid():N}@LOGICFIT.TEST",
            PasswordHash = "test-password-hash",
            EmailVerifiedAt = fixture.Clock.UtcNow
        };
        var owner = new User
        {
            TenantId = tenant.Id,
            IdentityAccountId = identity.Id,
            Email = identity.Email,
            PasswordHash = identity.PasswordHash,
            Role = UserRole.Owner,
            IsActive = true
        };
        var membership = new WorkspaceMembership
        {
            TenantId = tenant.Id,
            IdentityAccountId = identity.Id,
            UserId = owner.Id,
            Role = UserRole.Owner,
            Status = WorkspaceMembershipStatus.PendingPlatformApproval
        };

        fixture.Db.Tenants.Add(tenant);
        fixture.Db.IdentityAccounts.Add(identity);
        fixture.Db.Set<User>().Add(owner);
        fixture.Db.WorkspaceMemberships.Add(membership);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        var issuer = new IdentityWorkspaceSessionIssuer(fixture.Db, fixture.Clock, fixture.CurrentUser);
        var result = await issuer.IssueAsync(identity.Id);

        Assert.Single(result.ActiveWorkspaces);
        Assert.Equal(tenant.Id, result.ActiveWorkspaces[0].WorkspaceId);
        Assert.False(result.RequiresWorkspaceSelection);

        fixture.Db.ChangeTracker.Clear();
        var persistedMembership = await fixture.Db.WorkspaceMemberships
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == membership.Id);
        Assert.Equal(WorkspaceMembershipStatus.Active, persistedMembership.Status);
        Assert.Equal(fixture.Clock.UtcNow, persistedMembership.ApprovedAt);
        Assert.Equal("identity-login-reconciliation", persistedMembership.ApprovedBy);
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; }
        public TestCurrentUserService CurrentUser { get; }
        public FixedClock Clock { get; }

        private TestFixture(ApplicationDbContext db, TestCurrentUserService currentUser, FixedClock clock)
            => (Db, CurrentUser, Clock) = (db, currentUser, clock);

        public static async Task<TestFixture> CreateAsync()
        {
            var databaseName = $"LogicFitIdentityWorkspaceSessionIssuerTests_{Guid.NewGuid():N}";
            var baseConnectionString = Environment.GetEnvironmentVariable("LOGICFIT_TEST_CONNECTION_STRING")
                ?? "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
            var connectionString = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = databaseName
            }.ConnectionString;
            var currentUser = new TestCurrentUserService();
            var clock = new FixedClock();
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            var db = new ApplicationDbContext(dbOptions, new TestTenantService(), currentUser, clock);
            await db.Database.EnsureCreatedAsync();
            return new TestFixture(db, currentUser, clock);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.Database.EnsureDeletedAsync();
            await Db.DisposeAsync();
            SqlConnection.ClearAllPools();
        }
    }

    private sealed class TestTenantService : ITenantService
    {
        public Guid? CurrentTenantId => null;
        public Task SetTenantAsync(Guid tenantId) => Task.CompletedTask;
        public Task SetTenantBySubdomainAsync(string subdomain) => Task.CompletedTask;
        public Task<bool> SetTenantByCustomDomainAsync(string host) => Task.FromResult(false);
        public Task<bool> TenantExistsAsync(Guid tenantId) => Task.FromResult(false);
        public Task<Guid?> ResolveTenantIdAsync(string identifier) => Task.FromResult<Guid?>(null);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public string? UserId => null;
        public string? UserName => null;
        public Guid? TenantId => null;
        public bool IsAuthenticated => false;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "LogicFit.Tests";
    }

    private sealed class FixedClock : IDateTimeService
    {
        public DateTime Now => UtcNow.ToLocalTime();
        public DateTime UtcNow { get; } = new(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);
    }
}
