using System.Data;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Idempotently imports operator-registered databases into DatabaseResources. Secret material is
/// protected at rest and is never included in logs or API DTOs.
/// </summary>
public sealed class DatabaseResourceSeeder(
    PlatformDbContext db,
    IConnectionStringProtector protector,
    IOptions<DatabaseResourcePoolOptions> options,
    ILogger<DatabaseResourceSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.SeedConfiguredResources || settings.Resources.Count == 0)
        {
            logger.LogInformation("No server-configured tenant database resources were supplied.");
            return;
        }

        ValidateDefinitions(settings.Resources);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        foreach (var definition in settings.Resources)
        {
            var provider = string.IsNullOrWhiteSpace(definition.Provider)
                ? "ManualMonster"
                : definition.Provider.Trim();
            var databaseName = definition.DatabaseName.Trim();
            var existing = await db.DatabaseResources
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(
                    x => x.Provider == provider && x.DatabaseName == databaseName,
                    cancellationToken);

            var connectionString = NormalizeConnectionString(definition.ConnectionString, databaseName);
            var encryptedConnectionString = protector.Protect(connectionString);
            var serverKey = ResolveServerKey(definition, connectionString);

            if (existing is null)
            {
                db.DatabaseResources.Add(new DatabaseResource
                {
                    Provider = provider,
                    DatabaseName = databaseName,
                    ServerKey = serverKey,
                    EncryptedConnectionString = encryptedConnectionString,
                    Status = DatabaseResourceStatus.Available
                });
                continue;
            }

            // A newly configured resource may repair a missing encrypted value. Once a resource
            // is Reserved/Provisioning/Assigned, its connection material belongs to that tenant
            // and must not be silently replaced by a startup configuration change.
            if (string.IsNullOrWhiteSpace(existing.EncryptedConnectionString))
            {
                existing.EncryptedConnectionString = encryptedConnectionString;
                existing.ServerKey ??= serverKey;
            }
            else if (existing.Status == DatabaseResourceStatus.Available &&
                     !ConnectionStringsEquivalent(existing.EncryptedConnectionString, connectionString))
            {
                existing.EncryptedConnectionString = encryptedConnectionString;
                existing.ServerKey = serverKey;
            }
            else if (string.IsNullOrWhiteSpace(existing.ServerKey))
            {
                existing.ServerKey = serverKey;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Verified {ResourceCount} configured tenant database resource(s) in the platform pool.",
            settings.Resources.Count);
    }

    private void ValidateDefinitions(IReadOnlyCollection<DatabaseResourceDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.DatabaseName) ||
                string.IsNullOrWhiteSpace(definition.ConnectionString))
                throw new InvalidOperationException(
                    "Every DatabaseResourcePool resource requires DatabaseName and ConnectionString.");

            _ = NormalizeConnectionString(definition.ConnectionString, definition.DatabaseName.Trim());
        }
    }

    private static string NormalizeConnectionString(string value, string expectedDatabaseName)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(value);
            if (!string.Equals(builder.InitialCatalog, expectedDatabaseName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"The configured connection string database does not match '{expectedDatabaseName}'.");

            return builder.ConnectionString;
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"The configured connection string for '{expectedDatabaseName}' is invalid.", exception);
        }
    }

    private static string? ResolveServerKey(DatabaseResourceDefinition definition, string connectionString)
    {
        if (!string.IsNullOrWhiteSpace(definition.ServerKey))
            return definition.ServerKey.Trim();

        var dataSource = new SqlConnectionStringBuilder(connectionString).DataSource;
        return string.IsNullOrWhiteSpace(dataSource) ? null : dataSource.Trim();
    }

    private bool ConnectionStringsEquivalent(string protectedExisting, string configured)
    {
        try
        {
            var existing = protector.Unprotect(protectedExisting);
            return string.Equals(
                NormalizeConnectionString(existing, new SqlConnectionStringBuilder(configured).InitialCatalog),
                configured,
                StringComparison.Ordinal);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A rotated data-protection key or a legacy malformed value is repaired only while the
            // resource is Available; the caller never needs the exception details or secret value.
            logger.LogWarning("Could not compare the protected connection for an Available database resource; it will be refreshed.");
            return false;
        }
    }
}
