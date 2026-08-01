using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogicFit.Infrastructure.Persistence;

/// <summary>
/// Brings the connected database to the schema represented by the migrations compiled into the
/// deployed API. A SQL Server application lock serializes this work across IIS workers and hosts.
/// </summary>
public sealed class StartupDatabaseMigrator
{
    private const string MigrationLockResource = "LogicFit:EFCoreMigrations";

    private readonly ApplicationDbContext _dbContext;
    private readonly StartupDatabaseMigrationOptions _options;
    private readonly ILogger<StartupDatabaseMigrator> _logger;

    public StartupDatabaseMigrator(
        ApplicationDbContext dbContext,
        IOptions<StartupDatabaseMigrationOptions> options,
        ILogger<StartupDatabaseMigrator> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning(
                "Automatic startup migrations are disabled by Database:StartupMigrations:Enabled. " +
                "The operator is responsible for applying every pending migration before startup.");
            return;
        }

        if (!_dbContext.Database.IsRelational())
        {
            _logger.LogInformation("Skipping startup migrations for a non-relational database provider.");
            return;
        }

        var previousCommandTimeout = _dbContext.Database.GetCommandTimeout();
        var connection = _dbContext.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        var lockAcquired = false;

        _dbContext.Database.SetCommandTimeout(_options.CommandTimeoutSeconds);

        try
        {
            if (openedHere)
                await _dbContext.Database.OpenConnectionAsync(cancellationToken);

            if (IsSqlServer())
            {
                await AcquireSqlServerMigrationLockAsync(connection, cancellationToken);
                lockAcquired = true;
            }

            var pendingMigrations = (await _dbContext.Database
                    .GetPendingMigrationsAsync(cancellationToken))
                .ToArray();

            if (pendingMigrations.Length == 0)
            {
                _logger.LogInformation("Database schema is current; no pending migrations were found.");
                return;
            }

            _logger.LogWarning(
                "Applying {MigrationCount} pending database migration(s): {MigrationIds}",
                pendingMigrations.Length,
                pendingMigrations);

            await _dbContext.Database.MigrateAsync(cancellationToken);

            var remainingMigrations = (await _dbContext.Database
                    .GetPendingMigrationsAsync(cancellationToken))
                .ToArray();

            if (remainingMigrations.Length != 0)
            {
                throw new InvalidOperationException(
                    $"Database migration verification failed. Pending migrations: {string.Join(", ", remainingMigrations)}");
            }

            _logger.LogInformation(
                "Successfully applied and verified {MigrationCount} database migration(s).",
                pendingMigrations.Length);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(
                exception,
                "Database startup migration failed. The API will remain stopped to protect schema consistency.");
            throw;
        }
        finally
        {
            if (lockAcquired && connection.State == ConnectionState.Open)
                await ReleaseSqlServerMigrationLockAsync(connection);

            if (openedHere && connection.State == ConnectionState.Open)
                await _dbContext.Database.CloseConnectionAsync();

            _dbContext.Database.SetCommandTimeout(previousCommandTimeout);
        }
    }

    private bool IsSqlServer()
        => _dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;

    private async Task AcquireSqlServerMigrationLockAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Session',
                @LockTimeout = @lockTimeoutMilliseconds;
            SELECT @result;
            """;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        AddParameter(command, "@resource", MigrationLockResource);
        AddParameter(command, "@lockTimeoutMilliseconds", checked(_options.LockTimeoutSeconds * 1000));

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (result < 0)
        {
            throw new TimeoutException(
                $"Could not acquire the database migration lock within {_options.LockTimeoutSeconds} seconds (result {result}).");
        }

        _logger.LogInformation("Acquired the database migration lock.");
    }

    private async Task ReleaseSqlServerMigrationLockAsync(DbConnection connection)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                EXEC sys.sp_releaseapplock
                    @Resource = @resource,
                    @LockOwner = 'Session';
                """;
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            AddParameter(command, "@resource", MigrationLockResource);
            await command.ExecuteNonQueryAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to release the database migration lock cleanly.");
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
