using LogicFit.Infrastructure.Persistence;
using Xunit;

namespace LogicFit.Tests;

public sealed class DatabaseResourcePoolOptionsTests
{
    [Fact]
    public void Accepts_unique_configured_resources()
    {
        var options = new DatabaseResourcePoolOptions
        {
            Resources =
            [
                new DatabaseResourceDefinition
                {
                    DatabaseName = "db-one",
                    ConnectionString = "Server=server;Database=db-one;User Id=user;Password=password;"
                },
                new DatabaseResourceDefinition
                {
                    DatabaseName = "db-two",
                    ConnectionString = "Server=server;Database=db-two;User Id=user;Password=password;"
                }
            ]
        };

        Assert.True(DatabaseResourcePoolOptions.IsValid(options));
    }

    [Fact]
    public void Rejects_duplicate_provider_and_database_entries()
    {
        var options = new DatabaseResourcePoolOptions
        {
            Resources =
            [
                new DatabaseResourceDefinition
                {
                    DatabaseName = "db-one",
                    ConnectionString = "Server=server;Database=db-one;User Id=user;Password=password;"
                },
                new DatabaseResourceDefinition
                {
                    DatabaseName = "db-one",
                    ConnectionString = "Server=server;Database=db-one;User Id=user;Password=password;"
                }
            ]
        };

        Assert.False(DatabaseResourcePoolOptions.IsValid(options));
    }
}
