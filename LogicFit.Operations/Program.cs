using System.Security.Cryptography;
using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
    var status = backupService.GetStatus();
    if (!status.IsEnabled || !status.IsReady)
        throw new InvalidOperationException("The protected backup service is not ready.");

    var batch = await backupService.CreateBatchAsync(
        new BackupBatchRequest(
            BackupScope.FullSystem,
            IdempotencyKey: idempotencyKey,
            IncludePlatform: true),
        CancellationToken.None);

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
