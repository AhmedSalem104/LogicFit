using LogicFit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class PlatformDbContextContractTests
{
    [Fact]
    public void Constructor_uses_the_context_specific_options_type()
    {
        var constructor = typeof(PlatformDbContext).GetConstructor(
            new[] { typeof(DbContextOptions<PlatformDbContext>) });

        Assert.NotNull(constructor);
    }
}
