using LogicFit.Domain.Common;
using LogicFit.Domain.Common.Interfaces;
using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Entities;

public class User : AuditableEntity, ITenantEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Guid? PrimaryBranchId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    // Bumped whenever this user's roles/permissions change; embedded in the JWT as "perm_ver"
    // so issued tokens can be invalidated on the next refresh.
    public int PermissionsVersion { get; set; } = 0;
    public decimal WalletBalance { get; set; } = 0;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Password Reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual UserProfile? Profile { get; set; }
    public virtual ICollection<UserRoleAssignment> UserRoles { get; set; } = new List<UserRoleAssignment>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual Branch? PrimaryBranch { get; set; }
    public virtual ICollection<UserBranchAccess> BranchAccesses { get; set; } = new List<UserBranchAccess>();

    // As Coach
    public virtual ICollection<DietPlan> CoachDietPlans { get; set; } = new List<DietPlan>();
    public virtual ICollection<WorkoutProgram> CoachWorkoutPrograms { get; set; } = new List<WorkoutProgram>();
    public virtual ICollection<ClientSubscription> SalesSubscriptions { get; set; } = new List<ClientSubscription>();

    // As Client
    public virtual ICollection<DietPlan> ClientDietPlans { get; set; } = new List<DietPlan>();
    public virtual ICollection<WorkoutProgram> ClientWorkoutPrograms { get; set; } = new List<WorkoutProgram>();
    public virtual ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
    public virtual ICollection<MealLog> MealLogs { get; set; } = new List<MealLog>();
    public virtual ICollection<BodyMeasurement> BodyMeasurements { get; set; } = new List<BodyMeasurement>();
    public virtual ICollection<ClientSubscription> Subscriptions { get; set; } = new List<ClientSubscription>();

    // Coach-Client Relationships
    public virtual ICollection<CoachClient> Trainees { get; set; } = new List<CoachClient>(); // As Coach
    public virtual ICollection<CoachClient> AssignedCoaches { get; set; } = new List<CoachClient>(); // As Client

    // Wallet Transactions
    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
