using LogicFit.Domain.Common.Interfaces;
using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// The ownership lists are deliberately explicit.  They are the contract used while the
/// legacy shared context is being retired; adding a table requires changing the inventory and
/// the corresponding context, rather than silently making it available to both databases.
/// </summary>
public static class DbContextOwnership
{
    public static readonly IReadOnlySet<Type> PlatformEntities = new HashSet<Type>
    {
        typeof(Tenant), typeof(DatabaseResource), typeof(TenantDatabaseMapping), typeof(TenantBrandAsset), typeof(IdentityAccount),
        typeof(IdentityEmailActionToken), typeof(IdentityWorkspaceSession),
        typeof(WorkspaceMembership), typeof(WorkspaceInvite), typeof(WorkspaceClientJoinCode),
        typeof(ApplicationRequest), typeof(ApplicationRequestRevision),
        typeof(ApplicationTrackingSession), typeof(FreelanceWorkspaceProfile),
        typeof(Role), typeof(Permission), typeof(RolePermission), typeof(RefreshToken),
        typeof(OutboxMessage), typeof(JobExecutionLog), typeof(AuditLog),
        typeof(Plan), typeof(Feature), typeof(FeatureDependency), typeof(FeatureQuotaDefinition),
        typeof(PlanFeature), typeof(TenantSubscription), typeof(TenantFeature),
        typeof(SubscriptionFeatureSnapshot), typeof(TenantPaymentMethod), typeof(PaymentRequest), typeof(PaymentProof), typeof(ProvisioningJob),
        typeof(SubscriptionPayment), typeof(SubscriptionInvoice), typeof(TenantUsage),
        typeof(BackupBatch), typeof(DatabaseBackup), typeof(SensitiveActionGrant), typeof(TenantBackupExport), typeof(TenantBackupDownloadGrant)
    };

    public static readonly IReadOnlySet<Type> TenantEntities = new HashSet<Type>
    {
        typeof(User), typeof(UserProfile), typeof(NutrientDefinition), typeof(Food),
        typeof(FoodMicronutrient), typeof(Recipe), typeof(RecipeIngredient), typeof(DietPlan),
        typeof(DailyMeal), typeof(MealItem), typeof(MealLog), typeof(Muscle), typeof(Exercise),
        typeof(WorkoutProgram), typeof(ProgramRoutine), typeof(RoutineExercise),
        typeof(WorkoutSession), typeof(SessionSet), typeof(BodyMeasurement),
        typeof(SubscriptionPlan), typeof(ClientSubscription), typeof(SubscriptionFreeze),
        typeof(CoachClient), typeof(ExerciseSecondaryMuscle), typeof(Notification),
        typeof(Attendance), typeof(StaffAttendance), typeof(Appointment),
        typeof(ChatConversation), typeof(ChatMessage), typeof(Challenge), typeof(ClientChallenge),
        typeof(Branch), typeof(BranchOperatingHours), typeof(UserBranchAccess),
        typeof(MembershipCard), typeof(GateAccessLog), typeof(Room), typeof(Equipment),
        typeof(MaintenanceRecord), typeof(ExpenseCategory), typeof(Expense), typeof(Invoice),
        typeof(InvoiceItem), typeof(Payment), typeof(Coupon), typeof(CouponUsage),
        typeof(TaxSetting), typeof(GroupClass), typeof(ClassSchedule), typeof(ClassEnrollment),
        typeof(ProductCategory), typeof(Product), typeof(StockItem), typeof(StockMovement),
        typeof(Supplier), typeof(PurchaseOrder), typeof(PurchaseOrderItem), typeof(Sale),
        typeof(SaleItem), typeof(EmployeeProfile), typeof(EmployeeBranch), typeof(Shift),
        typeof(ShiftAssignment), typeof(LeaveRequest), typeof(Commission),
        typeof(CommissionRule), typeof(PayrollRun), typeof(PayrollItem),
        typeof(Role), typeof(Permission), typeof(RolePermission), typeof(UserRoleAssignment), typeof(AuditLog),
        typeof(WalletTransaction), typeof(OutboxMessage), typeof(JobExecutionLog)
    };

    /// <summary>
    /// These are contracts/projections that intentionally have a representation in both stores.
    /// They never create a cross-database foreign key; each store owns its local rows.
    /// </summary>
    public static readonly IReadOnlySet<Type> SharedContractEntities = new HashSet<Type>
    {
        typeof(Role), typeof(Permission), typeof(RolePermission), typeof(AuditLog),
        typeof(OutboxMessage), typeof(JobExecutionLog)
    };

    public static bool IsConfigurationFor(Type configurationType, IReadOnlySet<Type> ownedEntities)
        => configurationType.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>) &&
            ownedEntities.Contains(i.GenericTypeArguments[0]));

    public static void ApplyOwnedConfigurations(ModelBuilder builder, IReadOnlySet<Type> ownedEntities)
        => builder.ApplyConfigurationsFromAssembly(
            typeof(DbContextOwnership).Assembly,
            configurationType => IsConfigurationFor(configurationType, ownedEntities));

    public static void ConfigureTenantQueryBoundary(ModelBuilder builder, Guid tenantId)
    {
        foreach (var entityType in TenantEntities.Where(t => typeof(ITenantEntity).IsAssignableFrom(t)))
        {
            var method = typeof(DbContextOwnership)
                .GetMethod(nameof(ApplyTenantQueryFilter), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                .MakeGenericMethod(entityType);
            method.Invoke(null, new object[] { builder, tenantId });
        }
    }

    private static void ApplyTenantQueryFilter<TEntity>(ModelBuilder builder, Guid tenantId)
        where TEntity : class, ITenantEntity
        => builder.Entity<TEntity>().HasQueryFilter(entity => entity.TenantId == tenantId);
}
