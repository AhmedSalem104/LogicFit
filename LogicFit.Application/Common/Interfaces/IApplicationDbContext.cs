using LogicFit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<NutrientDefinition> NutrientDefinitions { get; }
    DbSet<Food> Foods { get; }
    DbSet<FoodMicronutrient> FoodMicronutrients { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeIngredient> RecipeIngredients { get; }
    DbSet<DietPlan> DietPlans { get; }
    DbSet<DailyMeal> DailyMeals { get; }
    DbSet<MealItem> MealItems { get; }
    DbSet<MealLog> MealLogs { get; }
    DbSet<Muscle> Muscles { get; }
    DbSet<Exercise> Exercises { get; }
    DbSet<ExerciseSecondaryMuscle> ExerciseSecondaryMuscles { get; }
    DbSet<WorkoutProgram> WorkoutPrograms { get; }
    DbSet<ProgramRoutine> ProgramRoutines { get; }
    DbSet<RoutineExercise> RoutineExercises { get; }
    DbSet<WorkoutSession> WorkoutSessions { get; }
    DbSet<SessionSet> SessionSets { get; }
    DbSet<BodyMeasurement> BodyMeasurements { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<ClientSubscription> ClientSubscriptions { get; }
    DbSet<SubscriptionFreeze> SubscriptionFreezes { get; }
    DbSet<CoachClient> CoachClients { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<WalletTransaction> WalletTransactions { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Attendance> Attendances { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<ChatConversation> ChatConversations { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<Challenge> Challenges { get; }
    DbSet<ClientChallenge> ClientChallenges { get; }
    DbSet<Branch> Branches { get; }
    DbSet<BranchOperatingHours> BranchOperatingHours { get; }
    DbSet<UserBranchAccess> UserBranchAccesses { get; }
    DbSet<MembershipCard> MembershipCards { get; }
    DbSet<GateAccessLog> GateAccessLogs { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Equipment> Equipment { get; }
    DbSet<MaintenanceRecord> MaintenanceRecords { get; }
    DbSet<ExpenseCategory> ExpenseCategories { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<CouponUsage> CouponUsages { get; }
    DbSet<TaxSetting> TaxSettings { get; }
    DbSet<GroupClass> GroupClasses { get; }
    DbSet<ClassSchedule> ClassSchedules { get; }
    DbSet<ClassEnrollment> ClassEnrollments { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<Product> Products { get; }
    DbSet<StockItem> StockItems { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderItem> PurchaseOrderItems { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleItem> SaleItems { get; }
    DbSet<EmployeeProfile> EmployeeProfiles { get; }
    DbSet<EmployeeBranch> EmployeeBranches { get; }
    DbSet<Shift> Shifts { get; }
    DbSet<ShiftAssignment> ShiftAssignments { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<Commission> Commissions { get; }
    DbSet<CommissionRule> CommissionRules { get; }
    DbSet<PayrollRun> PayrollRuns { get; }
    DbSet<PayrollItem> PayrollItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
