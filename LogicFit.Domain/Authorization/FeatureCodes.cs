namespace LogicFit.Domain.Authorization;

/// <summary>Catalog of feature-gate codes tied to plans (see PlanFeature / TenantFeature).</summary>
public static class FeatureCodes
{
    public const string POS = "POS";
    public const string Inventory = "Inventory";
    public const string AdvancedReports = "AdvancedReports";
    public const string MultiBranch = "MultiBranch";
    public const string WhiteLabel = "WhiteLabel";
    public const string EmployeeManagement = "EmployeeManagement";
    public const string FinanceModule = "FinanceModule";
    public const string ClientMobileApp = "ClientMobileApp";
    public const string CustomDomain = "CustomDomain";

    public static readonly IReadOnlyList<string> All = new[]
    {
        POS, Inventory, AdvancedReports, MultiBranch, WhiteLabel,
        EmployeeManagement, FinanceModule, ClientMobileApp, CustomDomain
    };
}
