using LogicFit.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace LogicFit.Infrastructure.Security;

public sealed class DataProtectionConnectionStringProtector : IConnectionStringProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionConnectionStringProtector(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector("LogicFit.Platform.TenantDatabaseMapping.v1");

    public string Protect(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return _protector.Protect(connectionString);
    }

    public string Unprotect(string protectedConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedConnectionString);
        return _protector.Unprotect(protectedConnectionString);
    }
}
