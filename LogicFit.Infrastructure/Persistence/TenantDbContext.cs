using LogicFit.Domain.Common.Interfaces;
using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Operational database context for exactly one Workspace.  A connection is resolved by the
/// server-side mapping; callers cannot supply a database name or connection string.
/// </summary>
public class TenantDbContext : DbContext
{
    public const string MigrationsAssemblyName = "LogicFit.Tenant.Migrations";
    public const string MigrationHistoryTable = "__TenantEFMigrationsHistory";

    public Guid TenantId { get; }

    public TenantDbContext(DbContextOptions options, Guid tenantId) : base(options)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("A non-empty tenant id is required for a TenantDbContext.", nameof(tenantId));

        TenantId = tenantId;
    }

    public DbSet<User> Users => Set<User>();
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
    public DbSet<AthleteCheckin> AthleteCheckins => Set<AthleteCheckin>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<ClientSubscription> ClientSubscriptions => Set<ClientSubscription>();
    public DbSet<SubscriptionFreeze> SubscriptionFreezes => Set<SubscriptionFreeze>();
    public DbSet<CoachClient> CoachClients => Set<CoachClient>();
    public DbSet<ExerciseSecondaryMuscle> ExerciseSecondaryMuscles => Set<ExerciseSecondaryMuscle>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<StaffAttendance> StaffAttendances => Set<StaffAttendance>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<ClientChallenge> ClientChallenges => Set<ClientChallenge>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<BranchOperatingHours> BranchOperatingHours => Set<BranchOperatingHours>();
    public DbSet<UserBranchAccess> UserBranchAccesses => Set<UserBranchAccess>();
    public DbSet<MembershipCard> MembershipCards => Set<MembershipCard>();
    public DbSet<GateAccessLog> GateAccessLogs => Set<GateAccessLog>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
    public DbSet<TaxSetting> TaxSettings => Set<TaxSetting>();
    public DbSet<GroupClass> GroupClasses => Set<GroupClass>();
    public DbSet<ClassSchedule> ClassSchedules => Set<ClassSchedule>();
    public DbSet<ClassEnrollment> ClassEnrollments => Set<ClassEnrollment>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<EmployeeBranch> EmployeeBranches => Set<EmployeeBranch>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollItem> PayrollItems => Set<PayrollItem>();
    public DbSet<Role> AppRoles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<JobExecutionLog> JobExecutionLogs => Set<JobExecutionLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(sql => sql
            .MigrationsAssembly(MigrationsAssemblyName)
            .MigrationsHistoryTable(MigrationHistoryTable));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        DbContextOwnership.ApplyOwnedConfigurations(modelBuilder, DbContextOwnership.TenantEntities);

        // These navigations point back to Platform DB records.  Their ids remain scalar values
        // in the tenant schema; cross-database foreign keys are never generated.
        foreach (var entityType in DbContextOwnership.PlatformEntities.Except(DbContextOwnership.SharedContractEntities))
            modelBuilder.Ignore(entityType);

        DbContextOwnership.ConfigureTenantQueryBoundary(modelBuilder, TenantId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceTenantBoundary();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnforceTenantBoundary();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnforceTenantBoundary()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.Entity.TenantId != TenantId)
                throw new InvalidOperationException("The entity TenantId does not match the TenantDbContext scope.");
        }
    }
}
