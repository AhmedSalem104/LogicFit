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
    public async Task Identity_login_repairs_pending_owner_membership_for_an_active_gym_only()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var gym = new Tenant
        {
            Name = "Active Gym",
            Subdomain = $"active-gym-{Guid.NewGuid():N}",
            WorkspaceType = WorkspaceType.Gym,
            Status = TenantStatus.Active
        };
        var ownerIdentity = NewIdentity("owner");
        var owner = NewUser(gym, ownerIdentity, UserRole.Owner);
        var ownerMembership = new WorkspaceMembership
        {
            TenantId = gym.Id,
            IdentityAccountId = ownerIdentity.Id,
            UserId = owner.Id,
            Role = UserRole.Owner,
            Status = WorkspaceMembershipStatus.PendingPlatformApproval
        };

        var pendingClientIdentity = NewIdentity("client");
        var pendingClient = NewUser(gym, pendingClientIdentity, UserRole.Client);
        var pendingClientMembership = new WorkspaceMembership
        {
            TenantId = gym.Id,
            IdentityAccountId = pendingClientIdentity.Id,
            UserId = pendingClient.Id,
            Role = UserRole.Client,
            Status = WorkspaceMembershipStatus.PendingWorkspaceApproval
        };

        fixture.Db.Tenants.Add(gym);
        fixture.Db.IdentityAccounts.AddRange(ownerIdentity, pendingClientIdentity);
        fixture.Db.Set<User>().AddRange(owner, pendingClient);
        fixture.Db.WorkspaceMemberships.AddRange(ownerMembership, pendingClientMembership);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        var issuer = new IdentityWorkspaceSessionIssuer(fixture.Db, fixture.Clock, fixture.CurrentUser);
        var response = await issuer.IssueAsync(ownerIdentity.Id);

        Assert.Single(response.ActiveWorkspaces);
        Assert.Equal(gym.Id, response.ActiveWorkspaces[0].WorkspaceId);
        Assert.False(response.RequiresWorkspaceSelection);

        fixture.Db.ChangeTracker.Clear();
        var persistedOwnerMembership = await fixture.Db.WorkspaceMemberships
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == ownerMembership.Id);
        var persistedClientMembership = await fixture.Db.WorkspaceMemberships
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == pendingClientMembership.Id);

        Assert.Equal(WorkspaceMembershipStatus.Active, persistedOwnerMembership.Status);
        Assert.Equal(fixture.Clock.UtcNow, persistedOwnerMembership.ApprovedAt);
        Assert.Equal("identity-login-reconciliation", persistedOwnerMembership.ApprovedBy);
        Assert.Equal(WorkspaceMembershipStatus.PendingWorkspaceApproval, persistedClientMembership.Status);
    }

    private static IdentityAccount NewIdentity(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return new IdentityAccount
        {
            FullName = $"{prefix} user",
            Email = $"{prefix}-{suffix}@logicfit.test",
            NormalizedEmail = $"{prefix}-{suffix}@LOGICFIT.TEST".ToUpperInvariant(),
            PasswordHash = "test-password-hash",
            EmailVerifiedAt = DateTime.UtcNow
        };
    }

    private static User NewUser(Tenant tenant, IdentityAccount identity, UserRole role) => new()
    {
        TenantId = tenant.Id,
        IdentityAccountId = identity.Id,
        Email = identity.Email,
        PasswordHash = identity.PasswordHash,
        Role = role,
        IsActive = true
    };

    private sealed class TestFixture : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; }
        public TestCurrentUserService CurrentUser { get; }
        public FixedClock Clock { get; }

        private TestFixture(ApplicationDbContext db, TestCurrentUserService currentUser, FixedClock clock)
            => (Db, CurrentUser, Clock) = (db, currentUser, clock);

        public static async Task<TestFixture> CreateAsync()
        {
            var databaseName = $"LogicFitIdentityWorkspaceSessionTests_{Guid.NewGuid():N}";
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
