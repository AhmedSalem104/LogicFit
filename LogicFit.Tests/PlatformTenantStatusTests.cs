using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Platform.Tenants.Commands.SetTenantStatus;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class PlatformTenantStatusTests
{
    [Theory]
    [InlineData(TenantStatus.PendingApproval)]
    [InlineData(TenantStatus.Active)]
    public async Task Activating_a_gym_activates_or_repairs_its_pending_owner_membership(
        TenantStatus initialTenantStatus)
    {
        await using var fixture = await TestFixture.CreateAsync();
        var tenant = new LogicFit.Domain.Entities.Tenant
        {
            Name = "Pending Gym",
            Subdomain = $"pending-gym-{Guid.NewGuid():N}",
            WorkspaceType = WorkspaceType.Gym,
            Status = initialTenantStatus
        };
        var ownerIdentity = new IdentityAccount
        {
            FullName = "Gym Owner",
            Email = $"owner-{Guid.NewGuid():N}@logicfit.test",
            NormalizedEmail = $"OWNER-{Guid.NewGuid():N}@LOGICFIT.TEST",
            PasswordHash = "test-password-hash",
            EmailVerifiedAt = fixture.Clock.UtcNow
        };
        var owner = new User
        {
            TenantId = tenant.Id,
            IdentityAccountId = ownerIdentity.Id,
            Email = ownerIdentity.Email,
            PasswordHash = ownerIdentity.PasswordHash,
            Role = UserRole.Owner,
            IsActive = true
        };
        var ownerMembership = new WorkspaceMembership
        {
            TenantId = tenant.Id,
            IdentityAccountId = ownerIdentity.Id,
            UserId = owner.Id,
            Role = UserRole.Owner,
            Status = WorkspaceMembershipStatus.PendingPlatformApproval
        };

        var clientIdentity = new IdentityAccount
        {
            FullName = "Pending Client",
            Email = $"client-{Guid.NewGuid():N}@logicfit.test",
            NormalizedEmail = $"CLIENT-{Guid.NewGuid():N}@LOGICFIT.TEST",
            PasswordHash = "test-password-hash",
            EmailVerifiedAt = fixture.Clock.UtcNow
        };
        var client = new User
        {
            TenantId = tenant.Id,
            IdentityAccountId = clientIdentity.Id,
            Email = clientIdentity.Email,
            PasswordHash = clientIdentity.PasswordHash,
            Role = UserRole.Client,
            IsActive = true
        };
        var clientMembership = new WorkspaceMembership
        {
            TenantId = tenant.Id,
            IdentityAccountId = clientIdentity.Id,
            UserId = client.Id,
            Role = UserRole.Client,
            Status = WorkspaceMembershipStatus.PendingWorkspaceApproval
        };

        fixture.Db.Tenants.Add(tenant);
        fixture.Db.IdentityAccounts.AddRange(ownerIdentity, clientIdentity);
        fixture.Db.Set<User>().AddRange(owner, client);
        fixture.Db.WorkspaceMemberships.AddRange(ownerMembership, clientMembership);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        var handler = new SetTenantStatusCommandHandler(fixture.Db, fixture.CurrentUser, fixture.Clock);
        var response = await handler.Handle(
            new SetTenantStatusCommand { TenantId = tenant.Id, Status = TenantStatus.Active },
            CancellationToken.None);

        fixture.Db.ChangeTracker.Clear();
        var persistedOwnerMembership = await fixture.Db.WorkspaceMemberships
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == ownerMembership.Id);
        var persistedClientMembership = await fixture.Db.WorkspaceMemberships
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == clientMembership.Id);
        var persistedTenant = await fixture.Db.Tenants
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == tenant.Id);

        Assert.Equal(TenantStatus.Active, response.Status);
        Assert.Equal(TenantStatus.Active, persistedTenant.Status);
        Assert.Equal(WorkspaceMembershipStatus.Active, persistedOwnerMembership.Status);
        Assert.Equal(fixture.Clock.UtcNow, persistedOwnerMembership.ApprovedAt);
        Assert.Equal(fixture.CurrentUser.UserId, persistedOwnerMembership.ApprovedBy);
        Assert.Equal(WorkspaceMembershipStatus.PendingWorkspaceApproval, persistedClientMembership.Status);
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
            var databaseName = $"LogicFitPlatformTenantStatusTests_{Guid.NewGuid():N}";
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
        public string? UserId => "platform-admin-test";
        public string? UserName => "platform-admin@logicfit.test";
        public Guid? TenantId => null;
        public bool IsAuthenticated => true;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "LogicFit.Tests";
    }

    private sealed class FixedClock : IDateTimeService
    {
        public DateTime Now => UtcNow.ToLocalTime();
        public DateTime UtcNow { get; } = new(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);
    }
}
