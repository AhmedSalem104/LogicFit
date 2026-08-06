using Microsoft.Data.SqlClient;
using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class ProductionDatabaseConnectivityTests
{
    [Fact]
    public void Protected_connection_can_execute_a_read_only_probe_with_the_application_sql_provider()
    {
        var connectionString = Environment.GetEnvironmentVariable("LOGICFIT_PRODUCTION_DB_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // The protected production probe is opt-in; ordinary CI never receives this secret.
            return;
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 15;

            var result = command.ExecuteScalar();
            Assert.Equal(1, Convert.ToInt32(result));
        }
        catch
        {
            throw new InvalidOperationException(
                "The protected production database probe failed with the application SQL provider.");
        }
    }

    [Fact]
    public async Task Protected_connection_can_report_pending_application_migrations_without_writing()
    {
        var connectionString = Environment.GetEnvironmentVariable("LOGICFIT_PRODUCTION_DB_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // The protected production probe is opt-in; ordinary CI never receives this secret.
            return;
        }

        try
        {
            await using var context = new ApplicationDbContextFactory().CreateDbContext(Array.Empty<string>());
            var pending = (await context.Database.GetPendingMigrationsAsync()).ToArray();
            Console.WriteLine($"Read-only pending application migrations: {pending.Length}.");
            if (pending.Length > 0)
                Console.WriteLine($"Pending migration ids: {string.Join(", ", pending)}.");
        }
        catch (Exception exception)
        {
            // Keep protected SQL details out of CI output while preserving the exception class for diagnosis.
            throw new InvalidOperationException(
                $"The protected pending-migration probe failed with {exception.GetType().Name}.");
        }
    }
}
