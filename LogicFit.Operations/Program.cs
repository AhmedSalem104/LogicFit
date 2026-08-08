using System.Security.Cryptography;
using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure;
using LogicFit.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

const string connectionVariable = "LOGICFIT_PRODUCTION_DB_CONNECTION";
const string outputVariable = "LOGICFIT_BACKUP_OUTPUT_ROOT";
const string resultVariable = "LOGICFIT_BACKUP_RESULT_PATH";
const string idempotencyVariable = "LOGICFIT_BACKUP_IDEMPOTENCY_KEY";

var connectionString = RequiredEnvironment(connectionVariable);
var outputRoot = Path.GetFullPath(RequiredEnvironment(outputVariable));
var resultPath = Path.GetFullPath(RequiredEnvironment(resultVariable));
var idempotencyKey = RequiredEnvironment(idempotencyVariable);

Directory.CreateDirectory(outputRoot);
Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);

var configurationValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
{
    ["ConnectionStrings:DefaultConnection"] = connectionString,
    ["JwtSettings:Secret"] = "protected-operator-process-only-secret-20260808",
    ["JwtSettings:Issuer"] = "LogicFit",
    ["Backup:Enabled"] = "true",
    ["Backup:StorageDirectory"] = "App_Data/PrivateBackups",
    ["Backup:RetentionDays"] = "30",
    ["Backup:RunAtUtc"] = "02:00",
    ["BackgroundJobs:Enabled"] = "false",
    ["DatabaseResourcePool:ProvisioningProvider"] = "ManualMonster",
    ["Storage:Provider"] = "local"
};

var host = new HostBuilder()
    .UseContentRoot(outputRoot)
    .UseEnvironment(Environments.Production)
    .ConfigureAppConfiguration((_, builder) =>
    {
        builder.Sources.Clear();
        builder.AddInMemoryCollection(configurationValues);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddInfrastructure(context.Configuration);
        services.AddScoped<ICurrentUserService, ProtectedOperatorUserService>();
    })
    .Build();

try
{
    await using var scope = host.Services.CreateAsyncScope();
    var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    var assignedResourceCount = await platformDb.DatabaseResources.AsNoTracking()
        .CountAsync(resource => resource.Status == DatabaseResourceStatus.Assigned);
    var activeMappingCount = await platformDb.TenantDatabaseMappings.AsNoTracking()
        .CountAsync(mapping => mapping.IsActive && !string.IsNullOrWhiteSpace(mapping.EncryptedConnectionString));
    var activeAssignedMappingCount = await platformDb.TenantDatabaseMappings.AsNoTracking()
        .Where(mapping => mapping.IsActive && !string.IsNullOrWhiteSpace(mapping.EncryptedConnectionString))
        .Join(
            platformDb.DatabaseResources.AsNoTracking()
                .Where(resource => resource.Status == DatabaseResourceStatus.Assigned),
            mapping => mapping.DatabaseResourceId,
            resource => resource.Id,
            (mapping, resource) => new { mapping, resource })
        .Join(
            platformDb.Tenants.AsNoTracking().IgnoreQueryFilters().Where(tenant => !tenant.IsDeleted),
            pair => pair.mapping.TenantId,
            tenant => tenant.Id,
            (pair, tenant) => pair)
        .CountAsync();
    Console.WriteLine(
        $"Protected platform inventory: assigned resources={assignedResourceCount}; active mappings={activeMappingCount}; active assigned mappings={activeAssignedMappingCount}.");

    var connectionProtector = scope.ServiceProvider.GetRequiredService<IConnectionStringProtector>();
    var protectedResources = await platformDb.DatabaseResources.AsNoTracking()
        .Where(resource =>
            resource.Status == DatabaseResourceStatus.Reserved ||
            resource.Status == DatabaseResourceStatus.Provisioning ||
            resource.Status == DatabaseResourceStatus.Assigned)
        .Where(resource => !string.IsNullOrWhiteSpace(resource.EncryptedConnectionString))
        .Select(resource => new
        {
            resource.Id,
            resource.Status,
            ProtectedValue = resource.EncryptedConnectionString!
        })
        .ToListAsync();
    foreach (var resource in protectedResources)
        Console.WriteLine(
            $"Protected resource {resource.Id}; status={resource.Status}; decrypt={GetDecryptionStatus(connectionProtector, resource.ProtectedValue)}.");

    var protectedMappings = await platformDb.TenantDatabaseMappings.AsNoTracking()
        .Where(mapping => mapping.IsActive && !string.IsNullOrWhiteSpace(mapping.EncryptedConnectionString))
        .Select(mapping => new
        {
            mapping.Id,
            mapping.TenantId,
            mapping.DatabaseResourceId,
            ProtectedValue = mapping.EncryptedConnectionString
        })
        .ToListAsync();
    foreach (var mapping in protectedMappings)
        Console.WriteLine(
            $"Protected mapping {mapping.Id}; tenant={mapping.TenantId}; resource={mapping.DatabaseResourceId}; decrypt={GetDecryptionStatus(connectionProtector, mapping.ProtectedValue)}.");

    var applicationDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var applicationAssignedMappings = applicationDb.TenantDatabaseMappings.AsNoTracking()
        .Where(mapping => mapping.IsActive && !string.IsNullOrWhiteSpace(mapping.EncryptedConnectionString))
        .Join(
            applicationDb.DatabaseResources.AsNoTracking()
                .Where(resource => resource.Status == DatabaseResourceStatus.Assigned),
            mapping => mapping.DatabaseResourceId,
            resource => resource.Id,
            (mapping, resource) => mapping)
        .Join(
            applicationDb.Tenants.AsNoTracking().IgnoreQueryFilters(),
            mapping => mapping.TenantId,
            tenant => tenant.Id,
            (mapping, tenant) => new { mapping, tenant });
    var applicationAssignedMappingCount = await applicationAssignedMappings.CountAsync();
    var applicationAssignedNonDeletedMappingCount = await applicationAssignedMappings
        .CountAsync(pair => !pair.tenant.IsDeleted);
    var applicationAssignedDeletedMappingCount = await applicationAssignedMappings
        .CountAsync(pair => pair.tenant.IsDeleted);
    Console.WriteLine(
        $"Application context inventory: assigned mappings={applicationAssignedMappingCount}; non-deleted={applicationAssignedNonDeletedMappingCount}; deleted={applicationAssignedDeletedMappingCount}.");

    var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
    var status = backupService.GetStatus();
    if (!status.IsEnabled || !status.IsReady)
        throw new InvalidOperationException("The protected backup service is not ready.");

    Console.WriteLine("Protected backup service is ready.");

    var batch = await backupService.CreateBatchAsync(
        new BackupBatchRequest(
            BackupScope.FullSystem,
            IdempotencyKey: idempotencyKey,
            IncludePlatform: true),
        CancellationToken.None);

    Console.WriteLine($"Protected FullSystem batch returned status {batch.Status} with {batch.Artifacts.Count} artifact(s).");

    if (!string.Equals(batch.Status, nameof(BackupBatchStatus.Completed), StringComparison.Ordinal) ||
        batch.Artifacts.Count == 0 ||
        batch.Artifacts.Any(artifact =>
            !string.Equals(artifact.Status, nameof(DatabaseBackupStatus.Completed), StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(artifact.StorageKey) ||
            artifact.SizeBytes <= 0 ||
            string.IsNullOrWhiteSpace(artifact.Sha256)))
    {
        throw new InvalidOperationException("The protected FullSystem backup did not complete for every target.");
    }

    var storageDirectory = Path.Combine(outputRoot, "App_Data", "PrivateBackups");
    var verifiedArtifacts = new List<VerifiedArtifact>();
    foreach (var artifact in batch.Artifacts)
    {
        var fileName = artifact.StorageKey!;
        if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal) ||
            (!fileName.EndsWith(".bacpac", StringComparison.OrdinalIgnoreCase) &&
             !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("The protected backup returned an unsafe storage key.");

        var path = Path.Combine(storageDirectory, fileName);
        if (!File.Exists(path))
            throw new InvalidOperationException("A protected backup artifact was not written to private storage.");

        var file = new FileInfo(path);
        await using var stream = File.OpenRead(path);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream));
        if (file.Length != artifact.SizeBytes ||
            !string.Equals(hash, artifact.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A protected backup artifact failed checksum verification.");

        verifiedArtifacts.Add(new VerifiedArtifact(file.Name, file.Length, hash));
    }

    if (string.IsNullOrWhiteSpace(batch.ManifestStorageKey) ||
        !File.Exists(Path.Combine(storageDirectory, batch.ManifestStorageKey)))
        throw new InvalidOperationException("The protected backup manifest was not written to private storage.");

    var result = new BackupResult(
        batch.Id,
        batch.Status,
        verifiedArtifacts.Count,
        verifiedArtifacts,
        batch.ManifestStorageKey);
    await File.WriteAllTextAsync(resultPath, JsonSerializer.Serialize(result));
}
catch (Exception exception)
{
    // The operator process must never echo a connection string or a provider exception payload.
    var sqlException = exception as SqlException ?? exception.GetBaseException() as SqlException;
    if (sqlException is not null)
        Console.Error.WriteLine($"Protected backup failed: SqlException number {sqlException.Number}, class {sqlException.Class}, state {sqlException.State}.");
    else if (exception is InvalidOperationException)
    {
        var category = exception.Message.Contains("No assigned tenant databases", StringComparison.Ordinal)
            ? "NoAssignedTenantDatabases"
            : exception.Message.Contains("mapping could not be resolved", StringComparison.OrdinalIgnoreCase)
                ? "MappingResolutionFailure"
                : exception.Message.Contains("backup", StringComparison.OrdinalIgnoreCase)
                    ? "BackupOperationRejected"
                    : "OperatorOperationRejected";
        Console.Error.WriteLine($"Protected backup failed: InvalidOperationException category {category}.");
    }
    else
        Console.Error.WriteLine($"Protected backup failed: {exception.GetType().Name}.");
    return 1;
}

return 0;

static string RequiredEnvironment(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"Required protected operator variable {name} is missing.");
    return value.Trim();
}

static string GetDecryptionStatus(IConnectionStringProtector protector, string protectedValue)
{
    try
    {
        return string.IsNullOrWhiteSpace(protector.Unprotect(protectedValue))
            ? "empty"
            : "ok";
    }
    catch (Exception exception) when (
        exception is ArgumentException or InvalidOperationException or System.Security.Cryptography.CryptographicException)
    {
        return exception.GetType().Name;
    }
}

file sealed class ProtectedOperatorUserService : ICurrentUserService
{
    public string? UserId => null;
    public string? UserName => "protected-operator";
    public Guid? TenantId => null;
    public bool IsAuthenticated => false;
    public string? IpAddress => null;
    public string? UserAgent => "protected-predeploy-backup";
}

file sealed record VerifiedArtifact(string FileName, long SizeBytes, string Sha256);

file sealed record BackupResult(
    Guid BatchId,
    string Status,
    int ArtifactCount,
    IReadOnlyList<VerifiedArtifact> Artifacts,
    string ManifestStorageKey);
