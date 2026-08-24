using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Coaches.Commands.CreateCoach;
using LogicFit.Application.Features.Identity;
using LogicFit.Application.Features.Identity.Commands.IdentitySignIn;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class CoachIdentityProvisioningTests
{
    [Fact]
    public async Task Creating_a_coach_creates_identity_and_active_workspace_membership()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var gym = new LogicFit.Domain.Entities.Tenant
        {
            Name = "Coach Identity Gym",
            Subdomain = $"coach-identity-{Guid.NewGuid():N}",
            WorkspaceType = WorkspaceType.Gym,
            Status = TenantStatus.Active
        };
        fixture.Db.Tenants.Add(gym);
        await fixture.Db.SaveChangesAsync();

        var command = new CreateCoachCommand
        {
            PhoneNumber = "01012345678",
            Email = "new-coach@logicfit.test",
            Password = "CoachPass1",
            FullName = "New Coach"
        };
        var handler = new CreateCoachCommandHandler(
            fixture.Db,
            new TestTenantService(gym.Id),
            new RecordingRbacService(),
            fixture.CurrentUser,
            fixture.Clock);

        var userId = await handler.Handle(command, CancellationToken.None);

        fixture.Db.ChangeTracker.Clear();
        var user = await fixture.Db.Set<User>().SingleAsync(x => x.Id == userId);
        var identity = await fixture.Db.IdentityAccounts.SingleAsync(x => x.NormalizedEmail == "NEW-COACH@LOGICFIT.TEST");
        var membership = await fixture.Db.WorkspaceMemberships
            .IgnoreQueryFilters()
            .SingleAsync(x => x.UserId == userId);

        Assert.Equal(identity.Id, user.IdentityAccountId);
        Assert.Equal(identity.Id, membership.IdentityAccountId);
        Assert.Equal(gym.Id, membership.TenantId);
        Assert.Equal(UserRole.Coach, membership.Role);
        Assert.Equal(WorkspaceMembershipStatus.Active, membership.Status);
        Assert.NotNull(identity.EmailVerifiedAt);
        Assert.True(BCrypt.Net.BCrypt.Verify(command.Password, identity.PasswordHash));
        Assert.Equal(identity.PasswordHash, user.PasswordHash);

        var signIn = new IdentitySignInCommandHandler(
            fixture.Db,
            new IdentityWorkspaceSessionIssuer(fixture.Db, fixture.Clock, fixture.CurrentUser),
            fixture.Clock,
            fixture.CurrentUser,
            new LegacyIdentityMigrationService(fixture.Db, new RecordingRbacService(), fixture.Clock));
        var response = await signIn.Handle(
            new IdentitySignInCommand(command.Email, command.Password),
            CancellationToken.None);

        Assert.Single(response.ActiveWorkspaces);
        Assert.Equal(gym.Id, response.ActiveWorkspaces[0].WorkspaceId);
    }

    [Fact]
    public async Task First_identity_login_migrates_an_existing_legacy_coach_after_password_match()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var gym = new LogicFit.Domain.Entities.Tenant
        {
            Name = "Legacy Coach Gym",
            Subdomain = $"legacy-coach-{Guid.NewGuid():N}",
            WorkspaceType = WorkspaceType.Gym,
            Status = TenantStatus.Active
        };
        var password = "LegacyPass1";
        var legacyCoach = new User
        {
            TenantId = gym.Id,
            Email = "legacy-coach@logicfit.test",
            PhoneNumber = "01098765432",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Coach,
            IsActive = true
        };
        fixture.Db.Tenants.Add(gym);
        fixture.Db.Set<User>().Add(legacyCoach);
        await fixture.Db.SaveChangesAsync();

        var signIn = new IdentitySignInCommandHandler(
            fixture.Db,
            new IdentityWorkspaceSessionIssuer(fixture.Db, fixture.Clock, fixture.CurrentUser),
            fixture.Clock,
            fixture.CurrentUser,
            new LegacyIdentityMigrationService(fixture.Db, new RecordingRbacService(), fixture.Clock));
        var response = await signIn.Handle(
            new IdentitySignInCommand(legacyCoach.Email, password),
            CancellationToken.None);

        var identity = await fixture.Db.IdentityAccounts
            .SingleAsync(x => x.NormalizedEmail == "LEGACY-COACH@LOGICFIT.TEST");
        var membership = await fixture.Db.WorkspaceMemberships
            .IgnoreQueryFilters()
            .SingleAsync(x => x.UserId == legacyCoach.Id);

        Assert.Equal(identity.Id, legacyCoach.IdentityAccountId);
        Assert.Equal(identity.Id, membership.IdentityAccountId);
        Assert.Equal(WorkspaceMembershipStatus.Active, membership.Status);
        Assert.Single(response.ActiveWorkspaces);
        Assert.Equal(gym.Id, response.ActiveWorkspaces[0].WorkspaceId);
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
            var databaseName = $"LogicFitCoachIdentityTests_{Guid.NewGuid():N}";
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
            var db = new ApplicationDbContext(dbOptions, new TestTenantService(null), currentUser, clock);
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

    private sealed class TestTenantService(Guid? tenantId) : ITenantService
    {
        public Guid? CurrentTenantId => tenantId;
        public Task SetTenantAsync(Guid id) => Task.CompletedTask;
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
