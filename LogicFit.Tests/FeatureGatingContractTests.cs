using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.ClientDashboard.Queries.GetMyDashboard;
using LogicFit.Application.Features.Employees.Commands.CreateEmployee;
using LogicFit.Application.Features.Expenses.Commands.CreateExpense;
using LogicFit.Application.Features.MealLogs.Commands.LogMeal;
using LogicFit.Application.Features.Products.Commands.CreateProduct;
using LogicFit.Application.Features.Reports.Queries.GetBranchComparisonReport;
using LogicFit.Application.Features.Reports.Queries.GetFinancialReport;
using LogicFit.Application.Features.Reports.Queries.GetOperationsDashboard;
using LogicFit.Application.Features.Sales.Commands.CheckoutSale;
using LogicFit.Application.Features.Stock.Commands.AdjustStock;
using LogicFit.Application.Features.Stock.Commands.TransferStock;
using LogicFit.Domain.Authorization;
using Xunit;

namespace LogicFit.Tests;

/// <summary>
/// Locks in the plan feature-gating wiring. The <see cref="SubscriptionGuardBehavior{TRequest,TResponse}"/>
/// enforces a gate only when a command implements <see cref="IRequireFeature"/>, so if a refactor drops
/// the interface or points it at the wrong code, the module silently becomes free again. These tests
/// fail fast on that regression.
///
/// NOTE: two feature gates are conditional and live in their handlers, not on the command, so they are
/// out of scope here and are covered by handler-level tests instead:
///   - CreateBranch  → MultiBranch  (only for the 2nd+ branch)
///   - UpdateGymProfile → WhiteLabel / CustomDomain (only when those fields are set)
/// </summary>
public class FeatureGatingContractTests
{
    [Fact]
    public void CheckoutSale_Requires_POS_Feature()
    {
        Assert.Equal(FeatureCodes.POS, new CheckoutSaleCommand().RequiredFeatureCode);
    }

    [Fact]
    public void CreateProduct_Requires_Inventory_Feature()
    {
        Assert.Equal(FeatureCodes.Inventory, new CreateProductCommand().RequiredFeatureCode);
    }

    [Fact]
    public void AdjustStock_Requires_Inventory_Feature()
    {
        Assert.Equal(FeatureCodes.Inventory, new AdjustStockCommand().RequiredFeatureCode);
    }

    [Fact]
    public void TransferStock_Requires_Inventory_Feature()
    {
        Assert.Equal(FeatureCodes.Inventory, new TransferStockCommand().RequiredFeatureCode);
    }

    [Fact]
    public void CreateExpense_Requires_FinanceModule_Feature()
    {
        Assert.Equal(FeatureCodes.FinanceModule, new CreateExpenseCommand().RequiredFeatureCode);
    }

    [Fact]
    public void CreateEmployee_Requires_EmployeeManagement_Feature()
    {
        Assert.Equal(FeatureCodes.EmployeeManagement, new CreateEmployeeCommand().RequiredFeatureCode);
    }

    [Fact]
    public void ClientDashboard_Requires_ClientMobileApp_Feature()
    {
        Assert.Equal(FeatureCodes.ClientMobileApp, new GetMyDashboardQuery().RequiredFeatureCode);
    }

    [Fact]
    public void LogMeal_Requires_ClientMobileApp_Feature()
    {
        Assert.Equal(FeatureCodes.ClientMobileApp, new LogMealCommand().RequiredFeatureCode);
    }

    [Theory]
    [InlineData(typeof(GetOperationsDashboardQuery))]
    [InlineData(typeof(GetFinancialReportQuery))]
    [InlineData(typeof(GetBranchComparisonReportQuery))]
    public void Advanced_Reports_Require_AdvancedReports_Feature(System.Type queryType)
    {
        var query = (IRequireFeature)System.Activator.CreateInstance(queryType)!;
        Assert.Equal(FeatureCodes.AdvancedReports, query.RequiredFeatureCode);
    }

    [Theory]
    [InlineData(typeof(CheckoutSaleCommand))]
    [InlineData(typeof(CreateProductCommand))]
    [InlineData(typeof(AdjustStockCommand))]
    [InlineData(typeof(TransferStockCommand))]
    [InlineData(typeof(CreateExpenseCommand))]
    [InlineData(typeof(CreateEmployeeCommand))]
    public void Gated_Commands_Implement_IRequireFeature(System.Type commandType)
    {
        Assert.True(typeof(IRequireFeature).IsAssignableFrom(commandType),
            $"{commandType.Name} must implement IRequireFeature so the pipeline enforces its plan gate.");
    }

    [Fact]
    public void Every_Declared_Feature_Code_Is_In_The_Catalog()
    {
        // Guards against a typo'd code that EnsureFeatureAsync would treat as "unknown" and silently skip.
        var declared = new[]
        {
            new CheckoutSaleCommand().RequiredFeatureCode,
            new CreateProductCommand().RequiredFeatureCode,
            new AdjustStockCommand().RequiredFeatureCode,
            new TransferStockCommand().RequiredFeatureCode,
            new CreateExpenseCommand().RequiredFeatureCode,
            new CreateEmployeeCommand().RequiredFeatureCode,
            new GetMyDashboardQuery().RequiredFeatureCode,
            new LogMealCommand().RequiredFeatureCode,
            new GetOperationsDashboardQuery().RequiredFeatureCode,
            new GetFinancialReportQuery().RequiredFeatureCode,
            new GetBranchComparisonReportQuery().RequiredFeatureCode,
        };

        foreach (var code in declared)
        {
            Assert.Contains(code, FeatureCodes.All);
        }
    }
}
