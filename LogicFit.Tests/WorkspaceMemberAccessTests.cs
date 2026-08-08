using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.WorkspaceMembers.Commands.ChangeWorkspaceMemberStatus;
using LogicFit.Application.Features.WorkspaceMembers.Commands.CreateWorkspaceMember;
using LogicFit.Application.Features.WorkspaceMembers.Commands.ResetWorkspaceMemberPassword;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class WorkspaceMemberAccessTests
{
    [Fact]
    public async Task Creating_a_member_creates_one_identity_membership_and_one_time_credentials()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var gym = await fixture.AddGymAsync("workspace-member-create");
        fixture.Tenant.CurrentTenantId = gym.Id;
        var handler = fixture.CreateCreateHandler();

        var result = await handler.Handle(new CreateWorkspaceMemberCommand
        {
            Email = "coach@workspace-member.test",
            PhoneNumber = "+201012345678",
            FullName = "Workspace Coach",
            Role = UserRole.Coach
        }, CancellationToken.None);

        Assert.True(result.NewIdentity);
        Assert.NotNull(result.OneTimeCredentials);
        Assert.Equal("coach@workspace-member.test", result.OneTimeCredentials!.Email);
        Assert.True(result.OneTimeCredentials.MustChangePassword);
        Assert.Equal("PasswordChangeRequired", result.Member.AccessStatus);
        Assert.True(result.Member.MustChangePassword);

        fixture.Db.ChangeTracker.Clear();
        var identity = await fixture.Db.IdentityAccounts.SingleAsync(x => x.NormalizedEmail == "COACH@WORKSPACE-MEMBER.TEST");
        var user = await fixture.Db.Set<User>().SingleAsync(x => x.IdentityAccountId == identity.Id && x.TenantId == gym.Id);
        var membership = await fixture.Db.WorkspaceMemberships.IgnoreQueryFilters().SingleAsync(x => x.UserId == user.Id);

        Assert.Equal(identity.Id, membership.IdentityAccountId);
        Assert.Equal(UserRole.Coach, membership.Role);
        Assert.Equal(WorkspaceMembershipStatus.Active, membership.Status);
        Assert.NotEqual(result.OneTimeCredentials.TemporaryPassword, identity.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(result.OneTimeCredentials.TemporaryPassword, identity.PasswordHash));

        await Assert.ThrowsAsync<LogicFit.Domain.Exceptions.ConflictException>(() => handler.Handle(
            new CreateWorkspaceMemberCommand
            {
                Email = "coach@workspace-member.test",
                PhoneNumber = "+201012345678",
                FullName = "Duplicate Coach",
                Role = UserRole.Coach
            }, CancellationToken.None));
    }

    [Fact]
    public async Task An_existing_identity_can_receive_a_membership_in_another_workspace_without_duplication()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var firstGym = await fixture.AddGymAsync("workspace-member-first");
        var secondGym = await fixture.AddGymAsync("workspace-member-second");
        var handler = fixture.CreateCreateHandler();

        fixture.Tenant.CurrentTenantId = firstGym.Id;
        var first = await handler.Handle(new CreateWorkspaceMemberCommand
        {
            Email = "shared@workspace-member.test",
            PhoneNumber = "+201098765432",
            FullName = "Shared Coach",
            Role = UserRole.Coach
        }, CancellationToken.None);

        fixture.Tenant.CurrentTenantId = secondGym.Id;
        var second = await handler.Handle(new CreateWorkspaceMemberCommand
        {
            Email = "shared@workspace-member.test",
            PhoneNumber = "+201098765432",
            FullName = "Shared Coach In Second Gym",
            Role = UserRole.Trainer
        }, CancellationToken.None);

        Assert.True(first.NewIdentity);
        Assert.False(second.NewIdentity);
        Assert.Null(second.OneTimeCredentials);
        Assert.Equal(first.Member.IdentityAccountId, second.Member.IdentityAccountId);
        Assert.NotEqual(first.Member.MembershipId, second.Member.MembershipId);
        Assert.Equal(UserRole.Trainer, second.Member.Role);

        var identityCount = await fixture.Db.IdentityAccounts.CountAsync(x => x.NormalizedEmail == "SHARED@WORKSPACE-MEMBER.TEST");
        var membershipCount = await fixture.Db.WorkspaceMemberships.IgnoreQueryFilters()
            .CountAsync(x => x.IdentityAccountId == first.Member.IdentityAccountId && !x.IsDeleted);
        Assert.Equal(1, identityCount);
        Assert.Equal(2, membershipCount);
    }

    [Fact]
    public async Task Removing_and_reactivating_a_member_preserves_the_global_identity()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var gym = await fixture.AddGymAsync("workspace-member-status");
        fixture.Tenant.CurrentTenantId = gym.Id;
        var create = await fixture.CreateCreateHandler().Handle(new CreateWorkspaceMemberCommand
        {
            Email = "status@workspace-member.test",
            FullName = "Status Coach",
            Role = UserRole.Coach
        }, CancellationToken.None);

        var statusHandler = fixture.CreateStatusHandler();
        var removed = await statusHandler.Handle(
            new ChangeWorkspaceMemberStatusCommand(create.Member.MembershipId, WorkspaceMemberStatusAction.Remove),
            CancellationToken.None);
        Assert.Equal("Removed", removed.AccessStatus);

        var activated = await statusHandler.Handle(
            new ChangeWorkspaceMemberStatusCommand(create.Member.MembershipId, WorkspaceMemberStatusAction.Activate),
            CancellationToken.None);
        Assert.Equal("PasswordChangeRequired", activated.AccessStatus);
        Assert.Equal(create.Member.IdentityAccountId, activated.IdentityAccountId);

        var reset = await fixture.CreateResetHandler().Handle(
            new ResetWorkspaceMemberPasswordCommand(create.Member.MembershipId), CancellationToken.None);
        Assert.NotNull(reset.OneTimeCredentials);
        Assert.Equal(create.Member.IdentityAccountId, reset.Member.IdentityAccountId);
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; }
        public MutableTenantService Tenant { get; }
        public TestCurrentUserService CurrentUser { get; }
        public FixedClock Clock { get; }

        private TestFixture(ApplicationDbContext db, MutableTenantService tenant, TestCurrentUserService currentUser, FixedClock clock)
            => (Db, Tenant, CurrentUser, Clock) = (db, tenant, currentUser, clock);

        public static async Task<TestFixture> CreateAsync()
        {
            var databaseName = $"LogicFitWorkspaceMemberTests_{Guid.NewGuid():N}";
            var baseConnectionString = Environment.GetEnvironmentVariable("LOGICFIT_TEST_CONNECTION_STRING")
                ?? "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
            var connectionString = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = databaseName }.ConnectionString;
            var tenant = new MutableTenantService();
            var currentUser = new TestCurrentUserService();
            var clock = new FixedClock();
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;
            var db = new ApplicationDbContext(dbOptions, tenant, currentUser, clock);
            await db.Database.EnsureCreatedAsync();
            return new TestFixture(db, tenant, currentUser, clock);
        }

        public async Task<Tenant> AddGymAsync(string prefix)
        {
            var gym = new Tenant
            {
                Name = prefix,
                Subdomain = $"{prefix}-{Guid.NewGuid():N}",
                WorkspaceType = WorkspaceType.Gym,
                Status = TenantStatus.Active
            };
            Db.Tenants.Add(gym);
            await Db.SaveChangesAsync();
            return gym;
        }

        public CreateWorkspaceMemberCommandHandler CreateCreateHandler() => new(
            Db, Tenant, new RecordingRbacService(), CurrentUser, Clock);

        public ChangeWorkspaceMemberStatusCommandHandler CreateStatusHandler() => new(
            Db, Tenant, new RecordingRbacService(), CurrentUser, Clock);

        public ResetWorkspaceMemberPasswordCommandHandler CreateResetHandler() => new(
            Db, Tenant, CurrentUser, Clock);

        public async ValueTask DisposeAsync()
        {
            await Db.Database.EnsureDeletedAsync();
            await Db.DisposeAsync();
            SqlConnection.ClearAllPools();
        }
    }

    private sealed class MutableTenantService : ITenantService
    {
        public Guid? CurrentTenantId { get; set; }
        public Task SetTenantAsync(Guid id) { CurrentTenantId = id; return Task.CompletedTask; }
        public Task SetTenantBySubdomainAsync(string subdomain) => Task.CompletedTask;
        public Task<bool> SetTenantByCustomDomainAsync(string host) => Task.FromResult(false);
        public Task<bool> TenantExistsAsync(Guid id) => Task.FromResult(false);
        public Task<Guid?> ResolveTenantIdAsync(string identifier) => Task.FromResult<Guid?>(null);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public string? UserId => "gym-owner";
        public string? UserName => "gym-owner";
        public Guid? TenantId => null;
        public bool IsAuthenticated => true;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "LogicFit.Tests";
    }

    private sealed class FixedClock : IDateTimeService
    {
        public DateTime Now => UtcNow.ToLocalTime();
        public DateTime UtcNow { get; } = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class RecordingRbacService : IRbacService
    {
        public Task<UserAuthorization> GetUserAuthorizationAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new UserAuthorization(Array.Empty<string>(), Array.Empty<string>()));

        public Task EnsureUserInRoleAsync(Guid userId, Guid? tenantId, string systemRoleName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
