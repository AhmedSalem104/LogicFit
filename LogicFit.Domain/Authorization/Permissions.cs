namespace LogicFit.Domain.Authorization;

/// <summary>
/// Single source of truth for permission codes. Used by [Authorize(Policy = ...)]
/// attributes and by RBAC seeding, so codes stay compile-time safe (no magic strings).
/// </summary>
public static class Permissions
{
    // --- Tenant (gym) permissions ---
    public const string ManageMembers = "ManageMembers";
    public const string ViewMembers = "ViewMembers";
    public const string ManageCoaches = "ManageCoaches";
    public const string ManageAttendance = "ManageAttendance";
    public const string ManageClientSubscriptions = "ManageClientSubscriptions";
    public const string ManagePOS = "ManagePOS";
    public const string ManageInventory = "ManageInventory";
    public const string ManageEmployees = "ManageEmployees";
    public const string ManageBranches = "ManageBranches";
    public const string ManageFinance = "ManageFinance";
    public const string ViewReports = "ViewReports";
    public const string ManageReports = "ManageReports";
    public const string ManageSettings = "ManageSettings";
    public const string ManageTenantBilling = "ManageTenantBilling";

    // --- Platform permissions ---
    public const string ManagePlatform = "ManagePlatform";
    public const string ManageTenants = "ManageTenants";
    public const string ManagePlans = "ManagePlans";
    public const string ManagePaymentRequests = "ManagePaymentRequests";
    public const string ManagePlatformReports = "ManagePlatformReports";
    public const string ManagePlatformBackups = "ManagePlatformBackups";

    /// <summary>All tenant-scoped permission codes.</summary>
    public static readonly IReadOnlyList<string> TenantPermissions = new[]
    {
        ManageMembers, ViewMembers, ManageCoaches, ManageAttendance, ManageClientSubscriptions,
        ManagePOS, ManageInventory, ManageEmployees, ManageBranches, ManageFinance,
        ViewReports, ManageReports, ManageSettings, ManageTenantBilling
    };

    /// <summary>All platform-scoped permission codes.</summary>
    public static readonly IReadOnlyList<string> PlatformPermissions = new[]
    {
        ManagePlatform, ManageTenants, ManagePlans, ManagePaymentRequests, ManagePlatformReports,
        ManagePlatformBackups
    };

    /// <summary>Every permission code in the system.</summary>
    public static readonly IReadOnlyList<string> All =
        TenantPermissions.Concat(PlatformPermissions).ToList();
}
