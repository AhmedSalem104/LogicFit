namespace LogicFit.Application.Features.Reports.DTOs;

// ===== Operations Dashboard =====
public class OperationsDashboardDto
{
    public int ActiveMembers { get; set; }
    public int TodayCheckIns { get; set; }
    public int CurrentlyInsideCount { get; set; } // Attendances without CheckOut
    public int ExpiringSubscriptionsIn7Days { get; set; }
    public int ExpiredSubscriptions { get; set; }

    public decimal MonthRevenue { get; set; }
    public decimal MonthExpenses { get; set; }
    public decimal MonthNetProfit => MonthRevenue - MonthExpenses;

    public decimal TodayRevenue { get; set; }
    public decimal TodayExpenses { get; set; }

    public int LowStockProductsCount { get; set; }
    public int EquipmentUnderMaintenanceCount { get; set; }
    public int PendingLeaveRequestsCount { get; set; }
    public int UnpaidInvoicesCount { get; set; }
    public decimal UnpaidInvoicesTotal { get; set; }

    public List<BranchKpiDto> BranchKpis { get; set; } = new();
}

public class BranchKpiDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public int CurrentlyInside { get; set; }
    public int TodayCheckIns { get; set; }
    public int ActiveMembers { get; set; }
    public decimal CapacityUsagePercent => Capacity.HasValue && Capacity.Value > 0
        ? Math.Round((decimal)CurrentlyInside / Capacity.Value * 100, 1)
        : 0;
}

// ===== Expenses Report =====
public class ExpensesReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalExpenses { get; set; }
    public int ExpensesCount { get; set; }
    public List<ExpenseByCategoryDto> ByCategory { get; set; } = new();
    public List<ExpenseByBranchDto> ByBranch { get; set; } = new();
    public List<ExpenseByMonthDto> ByMonth { get; set; } = new();
}

public class ExpenseByCategoryDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class ExpenseByBranchDto
{
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int Count { get; set; }
}

public class ExpenseByMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalAmount { get; set; }
    public int Count { get; set; }
}

// ===== POS Sales Report =====
public class PosSalesReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalDiscount { get; set; }
    public int SalesCount { get; set; }
    public int ItemsSold { get; set; }
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<SalesByCashierDto> ByCashier { get; set; } = new();
    public List<SalesByBranchDto> ByBranch { get; set; } = new();
    public List<SalesByPaymentDto> ByPaymentMethod { get; set; } = new();
}

public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesByCashierDto
{
    public Guid? CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public int SalesCount { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesByBranchDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int SalesCount { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesByPaymentDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

// ===== Stock Valuation Report =====
public class StockValuationReportDto
{
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalRetailValue { get; set; }
    public decimal PotentialProfit => TotalRetailValue - TotalCostValue;
    public int ProductsCount { get; set; }
    public int LowStockCount { get; set; }
    public List<StockProductValuationDto> Products { get; set; } = new();
}

public class StockProductValuationDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CostValue { get; set; }
    public decimal RetailValue { get; set; }
    public int MinStockLevel { get; set; }
    public bool IsLowStock { get; set; }
}

// ===== Payroll Summary =====
public class PayrollSummaryReportDto
{
    public int Year { get; set; }
    public int? Month { get; set; }
    public decimal TotalBaseSalaries { get; set; }
    public decimal TotalCommissions { get; set; }
    public decimal TotalBonuses { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetSalaries { get; set; }
    public int EmployeesPaid { get; set; }
    public int PendingCommissionsCount { get; set; }
    public decimal PendingCommissionsAmount { get; set; }
    public List<PayrollByBranchDto> ByBranch { get; set; } = new();
}

public class PayrollByBranchDto
{
    public Guid? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int EmployeesCount { get; set; }
    public decimal TotalNetSalaries { get; set; }
}

// ===== Class Attendance Report =====
public class ClassAttendanceReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalSchedulesHeld { get; set; }
    public int CancelledSchedulesCount { get; set; }
    public int TotalBookings { get; set; }
    public int TotalAttended { get; set; }
    public int TotalNoShows { get; set; }
    public int TotalCancellations { get; set; }
    public decimal AttendanceRatePercent { get; set; }
    public decimal AverageFillRatePercent { get; set; }
    public List<ClassPopularityDto> TopClasses { get; set; } = new();
    public List<CoachClassStatsDto> CoachStats { get; set; } = new();
}

public class ClassPopularityDto
{
    public Guid GroupClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SchedulesCount { get; set; }
    public int Bookings { get; set; }
    public int Attended { get; set; }
    public decimal AttendanceRatePercent { get; set; }
}

public class CoachClassStatsDto
{
    public Guid? CoachId { get; set; }
    public string CoachName { get; set; } = string.Empty;
    public int SchedulesCount { get; set; }
    public int TotalAttendance { get; set; }
}

// ===== Equipment Utilization =====
public class EquipmentUtilizationReportDto
{
    public int TotalEquipment { get; set; }
    public int ActiveCount { get; set; }
    public int UnderMaintenanceCount { get; set; }
    public int OutOfServiceCount { get; set; }
    public decimal TotalPurchaseValue { get; set; }
    public decimal TotalMaintenanceCost { get; set; }
    public int TotalMaintenanceRecords { get; set; }
    public int OpenMaintenanceCount { get; set; }
    public List<EquipmentCostDto> MostCostlyEquipment { get; set; } = new();
    public List<EquipmentByBranchDto> ByBranch { get; set; } = new();
}

public class EquipmentCostDto
{
    public Guid EquipmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaintenanceRecordsCount { get; set; }
    public decimal TotalMaintenanceCost { get; set; }
    public decimal? PurchasePrice { get; set; }
}

public class EquipmentByBranchDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Active { get; set; }
    public int UnderMaintenance { get; set; }
    public int OutOfService { get; set; }
}

// ===== Branch Comparison =====
public class BranchComparisonReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<BranchPerformanceDto> Branches { get; set; } = new();
}

public class BranchPerformanceDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int ActiveMembers { get; set; }
    public int CheckIns { get; set; }
    public decimal SubscriptionRevenue { get; set; }
    public decimal PosRevenue { get; set; }
    public decimal TotalRevenue => SubscriptionRevenue + PosRevenue;
    public decimal Expenses { get; set; }
    public decimal NetProfit => TotalRevenue - Expenses;
    public int ClassesHeld { get; set; }
    public int Employees { get; set; }
}

// ===== Commission Report =====
public class CommissionReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalEarned { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public int CommissionsCount { get; set; }
    public List<CommissionByEmployeeDto> ByEmployee { get; set; } = new();
    public List<CommissionBySourceDto> BySource { get; set; } = new();
}

public class CommissionByEmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal TotalEarned { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Pending { get; set; }
    public int Count { get; set; }
}

public class CommissionBySourceDto
{
    public string SourceType { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int Count { get; set; }
}
