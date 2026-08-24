namespace LogicFit.Infrastructure.Persistence;

public sealed class TenantDatabaseRoutingOptions
{
    public const string SectionName = "Database:TenantRouting";

    public bool Enabled { get; set; } = true;
    public bool FailClosedWithoutMapping { get; set; } = true;
}
