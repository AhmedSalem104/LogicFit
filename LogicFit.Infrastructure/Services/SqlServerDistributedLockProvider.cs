using System.Data;
using LogicFit.Application.Common.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogicFit.Infrastructure.Services;

/// <summary>
/// Uses a SQL Server session-owned application lock so scheduled work is singleton across
/// IIS workers and separately deployed API instances. The lease keeps its own connection open
/// until the lock is released.
/// </summary>
public sealed class SqlServerDistributedLockProvider(
    IConfiguration configuration,
    ILogger<SqlServerDistributedLockProvider> logger) : IDistributedLockProvider
{
    private const int LockTimeoutMilliseconds = 0;

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string resource,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        if (resource.Length > 255)
            throw new ArgumentException("The lock resource cannot exceed 255 characters.", nameof(resource));

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("The database connection is required for distributed background-job locks.");

        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);

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
            command.CommandTimeout = Math.Clamp(
                configuration.GetValue("BackgroundJobs:DistributedLockCommandTimeoutSeconds", 5),
                1,
                30);
            AddParameter(command, "@resource", SqlDbType.NVarChar, resource);
            AddParameter(command, "@lockTimeoutMilliseconds", SqlDbType.Int, LockTimeoutMilliseconds);

            var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            if (result < 0)
            {
                logger.LogDebug("Background-job lock {Resource} is owned by another instance.", resource);
                await connection.DisposeAsync();
                return null;
            }

            return new Lease(connection, resource, command.CommandTimeout, logger);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static void AddParameter(SqlCommand command, string name, SqlDbType type, object value)
    {
        var parameter = command.Parameters.Add(name, type);
        parameter.Value = value;
    }

    private sealed class Lease(
        SqlConnection connection,
        string resource,
        int commandTimeout,
        ILogger logger) : IAsyncDisposable
    {
        private int _released;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
                return;

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = """
                    EXEC sys.sp_releaseapplock
                        @Resource = @resource,
                        @LockOwner = 'Session';
                    """;
                command.CommandTimeout = commandTimeout;
                AddParameter(command, "@resource", SqlDbType.NVarChar, resource);
                await command.ExecuteNonQueryAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to release background-job lock {Resource} cleanly.", resource);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }
}
