using System.Data;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogicFit.Tests;

public sealed class WalletStockConcurrencyTests
{
    [Fact]
    public void Hot_paths_use_guarded_sql_updates_and_transactions()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var walletSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "LogicFit.Application",
            "Common",
            "Services",
            "WalletBalanceOperations.cs"));
        var stockSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "LogicFit.Application",
            "Common",
            "Services",
            "StockConcurrencyOperations.cs"));
        var checkoutSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "LogicFit.Application",
            "Features",
            "Sales",
            "Commands",
            "CheckoutSale",
            "CheckoutSaleCommandHandler.cs"));
        var refundSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "LogicFit.Application",
            "Features",
            "Subscriptions",
            "Commands",
            "CancelSubscription",
            "CancelSubscriptionCommandHandler.cs"));

        Assert.Contains("ExecuteUpdateAsync", walletSource);
        Assert.Contains("WalletBalance >= -delta", walletSource);
        Assert.Contains("ExecuteUpdateAsync", stockSource);
        Assert.Contains("item.Quantity >= quantity", stockSource);
        Assert.Contains("IsolationLevel.Serializable", checkoutSource);
        Assert.Contains("WalletBalanceOperations.ApplyAsync", refundSource);
        Assert.Contains("BeginTransactionAsync", refundSource);
    }

    [Fact]
    public async Task Concurrent_wallet_debits_cannot_overspend_or_lose_a_balance_update()
    {
        await using var fixture = await SqlFixture.CreateAsync();
        var tenant = new Tenant
        {
            Name = "Wallet concurrency test",
            Subdomain = $"wallet-{Guid.NewGuid():N}",
            Status = TenantStatus.Active
        };
        var user = new User
        {
            TenantId = tenant.Id,
            Email = $"wallet-{Guid.NewGuid():N}@logicfit.test",
            PasswordHash = "test-only",
            Role = UserRole.Client,
            WalletBalance = 100m,
            IsActive = true
        };

        fixture.Db.Tenants.Add(tenant);
        fixture.Db.Set<User>().Add(user);
        await fixture.Db.SaveChangesAsync();

        var results = await Task.WhenAll(
            TryDebitAsync(fixture.ConnectionString, tenant.Id, user.Id, 75m),
            TryDebitAsync(fixture.ConnectionString, tenant.Id, user.Id, 75m));

        Assert.Equal(1, results.Count(result => result));

        await using var verify = fixture.CreateDbContext();
        Assert.Equal(25m, await verify.Set<User>()
            .Where(item => item.Id == user.Id)
            .Select(item => item.WalletBalance)
            .SingleAsync());
        Assert.Equal(1, await verify.WalletTransactions.CountAsync(item => item.UserId == user.Id));
    }

    [Fact]
    public async Task Concurrent_stock_decrements_cannot_oversell_or_lose_quantity()
    {
        await using var fixture = await SqlFixture.CreateAsync();
        var tenant = new Tenant
        {
            Name = "Stock concurrency test",
            Subdomain = $"stock-{Guid.NewGuid():N}",
            Status = TenantStatus.Active
        };
        var branch = new Branch
        {
            TenantId = tenant.Id,
            Name = "Main branch"
        };
        var product = new Product
        {
            TenantId = tenant.Id,
            Name = "Concurrency product",
            SellingPrice = 10m,
            TrackStock = true
        };
        var stock = new StockItem
        {
            TenantId = tenant.Id,
            ProductId = product.Id,
            BranchId = branch.Id,
            Quantity = 10m
        };

        fixture.Db.Tenants.Add(tenant);
        fixture.Db.Branches.Add(branch);
        fixture.Db.Products.Add(product);
        fixture.Db.StockItems.Add(stock);
        await fixture.Db.SaveChangesAsync();

        var results = await Task.WhenAll(
            TryDecrementStockAsync(fixture.ConnectionString, tenant.Id, product.Id, branch.Id, 7m),
            TryDecrementStockAsync(fixture.ConnectionString, tenant.Id, product.Id, branch.Id, 7m));

        Assert.Equal(1, results.Count(result => result));

        await using var verify = fixture.CreateDbContext();
        Assert.Equal(3m, await verify.StockItems
            .Where(item => item.Id == stock.Id)
            .Select(item => item.Quantity)
            .SingleAsync());
        Assert.Equal(1, await verify.StockMovements.CountAsync(item =>
            item.ProductId == product.Id && item.BranchId == branch.Id));
    }

    private static async Task<bool> TryDebitAsync(
        string connectionString,
        Guid tenantId,
        Guid userId,
        decimal amount)
    {
        await using var db = SqlFixture.CreateDbContext(connectionString);
        await using var transaction = await db.BeginTransactionAsync();

        try
        {
            var balanceAfter = await WalletBalanceOperations.ApplyAsync(
                db,
                tenantId,
                userId,
                -amount);
            db.WalletTransactions.Add(new WalletTransaction
            {
                TenantId = tenantId,
                UserId = userId,
                Type = TransactionType.Payment,
                Amount = amount,
                BalanceAfter = balanceAfter,
                ReferenceType = "ConcurrencyTest"
            });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (ValidationException)
        {
            return false;
        }
    }

    private static async Task<bool> TryDecrementStockAsync(
        string connectionString,
        Guid tenantId,
        Guid productId,
        Guid branchId,
        decimal quantity)
    {
        await using var db = SqlFixture.CreateDbContext(connectionString);
        await using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable);

        var quantityAfter = await StockConcurrencyOperations.TryDecreaseExistingAsync(
            db,
            tenantId,
            productId,
            branchId,
            quantity,
            DateTime.UtcNow);
        if (!quantityAfter.HasValue)
            return false;

        db.StockMovements.Add(new StockMovement
        {
            TenantId = tenantId,
            ProductId = productId,
            BranchId = branchId,
            Type = StockMovementType.Out,
            Quantity = quantity,
            QuantityAfter = quantityAfter.Value,
            ReferenceType = "ConcurrencyTest",
            MovedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    private sealed class SqlFixture : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; }
        public string ConnectionString { get; }

        private SqlFixture(ApplicationDbContext db, string connectionString)
        {
            Db = db;
            ConnectionString = connectionString;
        }

        public static async Task<SqlFixture> CreateAsync()
        {
            var databaseName = $"LogicFitConcurrencyTests_{Guid.NewGuid():N}";
            var baseConnectionString = Environment.GetEnvironmentVariable("LOGICFIT_TEST_CONNECTION_STRING")
                ?? "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
            var connectionString = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = databaseName
            }.ConnectionString;
            var db = CreateDbContext(connectionString);
            await db.Database.EnsureCreatedAsync();
            return new SqlFixture(db, connectionString);
        }

        public ApplicationDbContext CreateDbContext() => CreateDbContext(ConnectionString);

        public static ApplicationDbContext CreateDbContext(string connectionString)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            return new ApplicationDbContext(
                options,
                new TestTenantService(),
                new TestCurrentUserService(),
                new TestDateTimeService());
        }

        public async ValueTask DisposeAsync()
        {
            await Db.Database.EnsureDeletedAsync();
            await Db.DisposeAsync();
        }
    }

    private sealed class TestDateTimeService : IDateTimeService
    {
        public DateTime Now => UtcNow.ToLocalTime();
        public DateTime UtcNow => DateTime.UtcNow;
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public string? UserId => null;
        public string? UserName => null;
        public Guid? TenantId => null;
        public bool IsAuthenticated => false;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "LogicFit.ConcurrencyTests";
    }

    private sealed class TestTenantService : ITenantService
    {
        public Guid? CurrentTenantId => null;
        public Task SetTenantAsync(Guid tenantId) => Task.CompletedTask;
        public Task SetTenantBySubdomainAsync(string subdomain) => Task.CompletedTask;
        public Task<bool> SetTenantByCustomDomainAsync(string host) => Task.FromResult(false);
        public Task<bool> TenantExistsAsync(Guid tenantId) => Task.FromResult(false);
        public Task<Guid?> ResolveTenantIdAsync(string identifier) => Task.FromResult<Guid?>(null);
    }
}
