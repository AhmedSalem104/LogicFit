namespace LogicFit.Domain.Authorization;

/// <summary>
/// Names of the built-in (system) roles seeded once with TenantId = null.
/// These map 1:1 to the legacy <see cref="Enums.UserRole"/> values plus the platform roles.
/// </summary>
public static class SystemRoles
{
    // Tenant roles
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Receptionist = "Receptionist";
    public const string Accountant = "Accountant";
    public const string Coach = "Coach";
    public const string Client = "Client";
    public const string Trainer = "Trainer";

    // Platform roles
    public const string PlatformOwner = "PlatformOwner";
    public const string PlatformAdmin = "PlatformAdmin";
}
