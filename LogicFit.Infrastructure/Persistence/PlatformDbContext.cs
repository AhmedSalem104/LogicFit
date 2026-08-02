using LogicFit.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Central Platform database context.  It owns identity, workspace metadata, applications,
/// billing and platform operations.  Tenant operational tables are intentionally not exposed.
/// </summary>
public class PlatformDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public const string MigrationsAssemblyName = "LogicFit.Platform.Migrations";
    public const string MigrationHistoryTable = "__PlatformEFMigrationsHistory";

    public PlatformDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Domain.Entities.Tenant> Tenants => Set<Domain.Entities.Tenant>();
    public DbSet<Domain.Entities.DatabaseResource> DatabaseResources => Set<Domain.Entities.DatabaseResource>();
    public DbSet<Domain.Entities.TenantDatabaseMapping> TenantDatabaseMappings => Set<Domain.Entities.TenantDatabaseMapping>();
    public DbSet<Domain.Entities.TenantBrandAsset> TenantBrandAssets => Set<Domain.Entities.TenantBrandAsset>();
    public DbSet<Domain.Entities.IdentityAccount> IdentityAccounts => Set<Domain.Entities.IdentityAccount>();
    public DbSet<Domain.Entities.IdentityEmailActionToken> IdentityEmailActionTokens => Set<Domain.Entities.IdentityEmailActionToken>();
    public DbSet<Domain.Entities.IdentityWorkspaceSession> IdentityWorkspaceSessions => Set<Domain.Entities.IdentityWorkspaceSession>();
    public DbSet<Domain.Entities.WorkspaceMembership> WorkspaceMemberships => Set<Domain.Entities.WorkspaceMembership>();
    public DbSet<Domain.Entities.WorkspaceInvite> WorkspaceInvites => Set<Domain.Entities.WorkspaceInvite>();
    public DbSet<Domain.Entities.WorkspaceClientJoinCode> WorkspaceClientJoinCodes => Set<Domain.Entities.WorkspaceClientJoinCode>();
    public DbSet<Domain.Entities.ApplicationRequest> ApplicationRequests => Set<Domain.Entities.ApplicationRequest>();
    public DbSet<Domain.Entities.ApplicationRequestRevision> ApplicationRequestRevisions => Set<Domain.Entities.ApplicationRequestRevision>();
    public DbSet<Domain.Entities.ApplicationTrackingSession> ApplicationTrackingSessions => Set<Domain.Entities.ApplicationTrackingSession>();
    public DbSet<Domain.Entities.FreelanceWorkspaceProfile> FreelanceWorkspaceProfiles => Set<Domain.Entities.FreelanceWorkspaceProfile>();
    public DbSet<Domain.Entities.Role> AppRoles => Set<Domain.Entities.Role>();
    public DbSet<Domain.Entities.Permission> Permissions => Set<Domain.Entities.Permission>();
    public DbSet<Domain.Entities.RolePermission> RolePermissions => Set<Domain.Entities.RolePermission>();
    public DbSet<Domain.Entities.RefreshToken> RefreshTokens => Set<Domain.Entities.RefreshToken>();
    public DbSet<Domain.Entities.OutboxMessage> OutboxMessages => Set<Domain.Entities.OutboxMessage>();
    public DbSet<Domain.Entities.JobExecutionLog> JobExecutionLogs => Set<Domain.Entities.JobExecutionLog>();
    public DbSet<Domain.Entities.AuditLog> AuditLogs => Set<Domain.Entities.AuditLog>();
    public DbSet<Domain.Entities.Plan> Plans => Set<Domain.Entities.Plan>();
    public DbSet<Domain.Entities.Feature> Features => Set<Domain.Entities.Feature>();
    public DbSet<Domain.Entities.FeatureDependency> FeatureDependencies => Set<Domain.Entities.FeatureDependency>();
    public DbSet<Domain.Entities.FeatureQuotaDefinition> FeatureQuotaDefinitions => Set<Domain.Entities.FeatureQuotaDefinition>();
    public DbSet<Domain.Entities.PlanFeature> PlanFeatures => Set<Domain.Entities.PlanFeature>();
    public DbSet<Domain.Entities.TenantSubscription> TenantSubscriptions => Set<Domain.Entities.TenantSubscription>();
    public DbSet<Domain.Entities.TenantFeature> TenantFeatures => Set<Domain.Entities.TenantFeature>();
    public DbSet<Domain.Entities.SubscriptionFeatureSnapshot> SubscriptionFeatureSnapshots => Set<Domain.Entities.SubscriptionFeatureSnapshot>();
    public DbSet<Domain.Entities.TenantPaymentMethod> TenantPaymentMethods => Set<Domain.Entities.TenantPaymentMethod>();
    public DbSet<Domain.Entities.PaymentRequest> PaymentRequests => Set<Domain.Entities.PaymentRequest>();
    public DbSet<Domain.Entities.SubscriptionPayment> SubscriptionPayments => Set<Domain.Entities.SubscriptionPayment>();
    public DbSet<Domain.Entities.SubscriptionInvoice> SubscriptionInvoices => Set<Domain.Entities.SubscriptionInvoice>();
    public DbSet<Domain.Entities.TenantUsage> TenantUsages => Set<Domain.Entities.TenantUsage>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Keep the history boundary attached to the context even when a caller only supplies
        // UseSqlServer(connectionString). The resolver still supplies the connection string.
        optionsBuilder.UseSqlServer(sql => sql
            .MigrationsAssembly(MigrationsAssemblyName)
            .MigrationsHistoryTable(MigrationHistoryTable));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        DbContextOwnership.ApplyOwnedConfigurations(modelBuilder, DbContextOwnership.PlatformEntities);

        // Domain users live in a Tenant DB.  Platform foreign keys such as WorkspaceMembership.UserId
        // remain scalar references; a cross-database FK is deliberately not created.
        foreach (var entityType in DbContextOwnership.TenantEntities.Except(DbContextOwnership.SharedContractEntities))
            modelBuilder.Ignore(entityType);
    }
}
