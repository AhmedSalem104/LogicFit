using System.Reflection;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LogicFit.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService) : base(options)
    {
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    // DbSets
    public DbSet<Tenant> Tenants => Set<Tenant>();
    DbSet<User> IApplicationDbContext.Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<NutrientDefinition> NutrientDefinitions => Set<NutrientDefinition>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<FoodMicronutrient> FoodMicronutrients => Set<FoodMicronutrient>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<DietPlan> DietPlans => Set<DietPlan>();
    public DbSet<DailyMeal> DailyMeals => Set<DailyMeal>();
    public DbSet<MealItem> MealItems => Set<MealItem>();
    public DbSet<MealLog> MealLogs => Set<MealLog>();
    public DbSet<Muscle> Muscles => Set<Muscle>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutProgram> WorkoutPrograms => Set<WorkoutProgram>();
    public DbSet<ProgramRoutine> ProgramRoutines => Set<ProgramRoutine>();
    public DbSet<RoutineExercise> RoutineExercises => Set<RoutineExercise>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<SessionSet> SessionSets => Set<SessionSet>();
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<ClientSubscription> ClientSubscriptions => Set<ClientSubscription>();
    public DbSet<SubscriptionFreeze> SubscriptionFreezes => Set<SubscriptionFreeze>();
    public DbSet<CoachClient> CoachClients => Set<CoachClient>();
    public DbSet<ExerciseSecondaryMuscle> ExerciseSecondaryMuscles => Set<ExerciseSecondaryMuscle>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations from assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure ApplicationUser decimal precision
        builder.Entity<ApplicationUser>()
            .Property(u => u.WalletBalance)
            .HasPrecision(18, 2);

        // Global Query Filters
        ApplyGlobalFilters(builder);
    }

    private void ApplyGlobalFilters(ModelBuilder builder)
    {
        // Tenant Filter for ITenantEntity
        builder.Entity<User>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<Recipe>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<DietPlan>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<DailyMeal>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<MealItem>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<MealLog>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<WorkoutProgram>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<ProgramRoutine>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<RoutineExercise>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<WorkoutSession>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<SessionSet>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<BodyMeasurement>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<SubscriptionPlan>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<ClientSubscription>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<SubscriptionFreeze>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<CoachClient>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));

        // Global Foods and Exercises (can be null TenantId for global or specific tenant)
        builder.Entity<Food>().HasQueryFilter(e => !e.IsDeleted && (e.TenantId == null || _tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
        builder.Entity<Exercise>().HasQueryFilter(e => !e.IsDeleted && (e.TenantId == null || _tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));

        // Non-tenant entities with soft delete only
        builder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<UserProfile>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<NutrientDefinition>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Muscle>().HasQueryFilter(e => !e.IsDeleted);

        // ApplicationUser filter
        builder.Entity<ApplicationUser>().HasQueryFilter(e => !e.IsDeleted && (_tenantService.CurrentTenantId == null || e.TenantId == _tenantService.CurrentTenantId));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries, cancellationToken);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            // Handle Auditable Entities
            if (entry.Entity is IAuditableEntity auditableEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditableEntity.CreatedAt = _dateTimeService.UtcNow;
                        auditableEntity.CreatedBy = _currentUserService.UserId;
                        break;
                    case EntityState.Modified:
                        auditableEntity.UpdatedAt = _dateTimeService.UtcNow;
                        auditableEntity.UpdatedBy = _currentUserService.UserId;
                        break;
                }
            }

            // Handle Tenant Entities
            if (entry.Entity is ITenantEntity tenantEntity && entry.State == EntityState.Added)
            {
                if (tenantEntity.TenantId == Guid.Empty && _tenantService.CurrentTenantId.HasValue)
                {
                    tenantEntity.TenantId = _tenantService.CurrentTenantId.Value;
                }
            }

            // Handle Soft Delete
            if (entry.Entity is ISoftDeletable softDeletable && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = _dateTimeService.UtcNow;
                softDeletable.DeletedBy = _currentUserService.UserId;
            }

            // Create Audit Entry
            var auditEntry = new AuditEntry(entry)
            {
                TableName = entry.Entity.GetType().Name,
                UserId = _currentUserService.UserId,
                TenantId = _tenantService.CurrentTenantId
            };

            auditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary)
                {
                    auditEntry.TemporaryProperties.Add(property);
                    continue;
                }

                var propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.Action = AuditAction.Create;
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;
                    case EntityState.Deleted:
                        auditEntry.Action = AuditAction.Delete;
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;
                    case EntityState.Modified:
                        if (property.IsModified && property.OriginalValue?.Equals(property.CurrentValue) != true)
                        {
                            auditEntry.Action = AuditAction.Update;
                            auditEntry.AffectedColumns.Add(propertyName);
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }

        return auditEntries.Where(e => e.HasChanges).ToList();
    }

    private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries, CancellationToken cancellationToken)
    {
        if (auditEntries.Count == 0)
            return;

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                    auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                else
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
            }

            AuditLogs.Add(auditEntry.ToAuditLog());
        }

        await base.SaveChangesAsync(cancellationToken);
    }
}

public class AuditEntry
{
    public AuditEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        Entry = entry;
    }

    public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; }
    public string? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public Dictionary<string, object?> KeyValues { get; } = new();
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<string> AffectedColumns { get; } = new();
    public List<Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry> TemporaryProperties { get; } = new();

    public bool HasChanges => OldValues.Count > 0 || NewValues.Count > 0;

    public AuditLog ToAuditLog()
    {
        return new AuditLog
        {
            TenantId = TenantId,
            UserId = UserId,
            Action = Action,
            EntityName = TableName,
            EntityId = JsonSerializer.Serialize(KeyValues),
            OldValues = OldValues.Count > 0 ? JsonSerializer.Serialize(OldValues) : null,
            NewValues = NewValues.Count > 0 ? JsonSerializer.Serialize(NewValues) : null,
            AffectedColumns = AffectedColumns.Count > 0 ? string.Join(",", AffectedColumns) : null,
            Timestamp = DateTime.UtcNow
        };
    }
}
