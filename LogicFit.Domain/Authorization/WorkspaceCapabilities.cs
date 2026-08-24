using LogicFit.Domain.Enums;

namespace LogicFit.Domain.Authorization;

/// <summary>
/// Capabilities describe the product surface available to a workspace type.
/// They are evaluated on the server from the persisted WorkspaceType; clients may
/// use the returned list for navigation, but it is never a security boundary.
/// </summary>
public static class WorkspaceCapabilities
{
    public const string GymExperience = "GymExperience";
    public const string FreelanceExperience = "FreelanceExperience";
    public const string GymFacilities = "GymFacilities";
    public const string GymAttendance = "GymAttendance";
    public const string GymStaff = "GymStaff";
    public const string GymInventory = "GymInventory";
    public const string GymPos = "GymPOS";
    public const string GymGateAccess = "GymGateAccess";
    public const string GymMembershipCards = "GymMembershipCards";
    public const string GymMembershipPlans = "GymMembershipPlans";
    public const string GymSettings = "GymSettings";
    public const string GymReports = "GymReports";
    public const string FreelanceTeam = "FreelanceTeam";
    public const string CoachingClients = "CoachingClients";
    public const string CoachingPrograms = "CoachingPrograms";
    public const string CoachingNutrition = "CoachingNutrition";
    public const string CoachingProgress = "CoachingProgress";
    public const string CoachingAppointments = "CoachingAppointments";
    public const string CoachingFinance = "CoachingFinance";
    public const string CoachingReports = "CoachingReports";
    public const string WorkspaceBilling = "WorkspaceBilling";
    public const string WorkspaceSettings = "WorkspaceSettings";
    public const string WorkspaceBackups = "WorkspaceBackups";

    public static readonly IReadOnlyList<string> All = new[]
    {
        GymExperience, FreelanceExperience, GymFacilities, GymAttendance, GymStaff,
        GymInventory, GymPos, GymGateAccess, GymMembershipCards, GymMembershipPlans,
        GymSettings, GymReports, FreelanceTeam, CoachingClients, CoachingPrograms,
        CoachingNutrition, CoachingProgress, CoachingAppointments, CoachingFinance,
        CoachingReports, WorkspaceBilling, WorkspaceSettings, WorkspaceBackups
    };

    private static readonly IReadOnlyList<string> Gym = new[]
    {
        GymExperience, CoachingClients, CoachingPrograms, CoachingNutrition,
        CoachingProgress, CoachingAppointments, CoachingFinance, CoachingReports,
        GymFacilities, GymAttendance, GymStaff, GymInventory, GymPos, GymGateAccess,
        GymMembershipCards, GymMembershipPlans, GymSettings, GymReports,
        WorkspaceBilling, WorkspaceSettings, WorkspaceBackups
    };

    private static readonly IReadOnlyList<string> FreelanceCoach = new[]
    {
        FreelanceExperience, FreelanceTeam, CoachingClients, CoachingPrograms,
        CoachingNutrition, CoachingProgress, CoachingAppointments, CoachingFinance,
        CoachingReports, WorkspaceBilling, WorkspaceSettings, WorkspaceBackups
    };

    public static IReadOnlyList<string> For(WorkspaceType workspaceType) => workspaceType switch
    {
        WorkspaceType.FreelanceCoach => FreelanceCoach,
        _ => Gym
    };

    public static bool IsAvailable(string capability, WorkspaceType workspaceType) =>
        For(workspaceType).Contains(capability, StringComparer.Ordinal);
}
