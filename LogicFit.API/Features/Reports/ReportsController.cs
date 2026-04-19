using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Application.Features.Reports.Queries.GetBranchComparisonReport;
using LogicFit.Application.Features.Reports.Queries.GetClassAttendanceReport;
using LogicFit.Application.Features.Reports.Queries.GetClientsReport;
using LogicFit.Application.Features.Reports.Queries.GetCoachDashboardReport;
using LogicFit.Application.Features.Reports.Queries.GetCoachTraineesReport;
using LogicFit.Application.Features.Reports.Queries.GetCommissionReport;
using LogicFit.Application.Features.Reports.Queries.GetDashboardReport;
using LogicFit.Application.Features.Reports.Queries.GetEquipmentUtilizationReport;
using LogicFit.Application.Features.Reports.Queries.GetExpensesReport;
using LogicFit.Application.Features.Reports.Queries.GetFinancialReport;
using LogicFit.Application.Features.Reports.Queries.GetOperationsDashboard;
using LogicFit.Application.Features.Reports.Queries.GetPayrollSummaryReport;
using LogicFit.Application.Features.Reports.Queries.GetPosSalesReport;
using LogicFit.Application.Features.Reports.Queries.GetStockValuationReport;
using LogicFit.Application.Features.Reports.Queries.GetSubscriptionsReport;
using LogicFit.Application.Features.Reports.Queries.GetTraineeProgressReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.Reports;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get dashboard summary report
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardReportDto>> GetDashboardReport()
    {
        var result = await _mediator.Send(new GetDashboardReportQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get detailed clients report
    /// </summary>
    [HttpGet("clients")]
    [ProducesResponseType(typeof(ClientsReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClientsReportDto>> GetClientsReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _mediator.Send(new GetClientsReportQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }

    /// <summary>
    /// Get subscriptions report
    /// </summary>
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(SubscriptionsReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionsReportDto>> GetSubscriptionsReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _mediator.Send(new GetSubscriptionsReportQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }

    /// <summary>
    /// Get financial report
    /// </summary>
    [HttpGet("financial")]
    [ProducesResponseType(typeof(FinancialReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinancialReportDto>> GetFinancialReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _mediator.Send(new GetFinancialReportQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(result);
    }

    // ===== Coach Reports =====

    /// <summary>
    /// Get coach dashboard report
    /// - Owner: can view any coach's dashboard (use coachId filter)
    /// - Coach: sees only their own dashboard
    /// </summary>
    [HttpGet("coach/dashboard")]
    [ProducesResponseType(typeof(CoachDashboardReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoachDashboardReportDto>> GetCoachDashboardReport(
        [FromQuery] Guid? coachId)
    {
        var result = await _mediator.Send(new GetCoachDashboardReportQuery
        {
            CoachId = coachId
        });
        return Ok(result);
    }

    /// <summary>
    /// Get coach trainees report with detailed stats
    /// - Owner: can view any coach's trainees
    /// - Coach: sees only their own trainees
    /// </summary>
    [HttpGet("coach/trainees")]
    [ProducesResponseType(typeof(CoachTraineesReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CoachTraineesReportDto>> GetCoachTraineesReport(
        [FromQuery] Guid? coachId)
    {
        var result = await _mediator.Send(new GetCoachTraineesReportQuery
        {
            CoachId = coachId
        });
        return Ok(result);
    }

    /// <summary>
    /// Get detailed progress report for a specific trainee
    /// - Owner: can view any trainee's progress
    /// - Coach: can only view their own trainees
    /// </summary>
    [HttpGet("coach/trainee/{clientId}")]
    [ProducesResponseType(typeof(TraineeProgressReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TraineeProgressReportDto>> GetTraineeProgressReport(Guid clientId)
    {
        var result = await _mediator.Send(new GetTraineeProgressReportQuery
        {
            ClientId = clientId
        });
        return Ok(result);
    }

    // ===== Phase 9: Operations / Gym Admin Reports =====

    /// <summary>
    /// Live operations dashboard: active members, today check-ins, revenue, expenses, capacity, low stock, etc.
    /// </summary>
    [HttpGet("operations-dashboard")]
    [ProducesResponseType(typeof(OperationsDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperationsDashboardDto>> GetOperationsDashboard()
        => Ok(await _mediator.Send(new GetOperationsDashboardQuery()));

    [HttpGet("expenses")]
    [ProducesResponseType(typeof(ExpensesReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExpensesReportDto>> GetExpensesReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? branchId)
        => Ok(await _mediator.Send(new GetExpensesReportQuery { FromDate = fromDate, ToDate = toDate, BranchId = branchId }));

    [HttpGet("pos-sales")]
    [ProducesResponseType(typeof(PosSalesReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PosSalesReportDto>> GetPosSalesReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? branchId,
        [FromQuery] int topProductsCount = 10)
        => Ok(await _mediator.Send(new GetPosSalesReportQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            BranchId = branchId,
            TopProductsCount = topProductsCount
        }));

    [HttpGet("stock-valuation")]
    [ProducesResponseType(typeof(StockValuationReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockValuationReportDto>> GetStockValuationReport([FromQuery] Guid? branchId)
        => Ok(await _mediator.Send(new GetStockValuationReportQuery { BranchId = branchId }));

    [HttpGet("payroll-summary")]
    [ProducesResponseType(typeof(PayrollSummaryReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PayrollSummaryReportDto>> GetPayrollSummaryReport(
        [FromQuery] int? year,
        [FromQuery] int? month)
        => Ok(await _mediator.Send(new GetPayrollSummaryReportQuery { Year = year, Month = month }));

    [HttpGet("class-attendance")]
    [ProducesResponseType(typeof(ClassAttendanceReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClassAttendanceReportDto>> GetClassAttendanceReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? branchId)
        => Ok(await _mediator.Send(new GetClassAttendanceReportQuery { FromDate = fromDate, ToDate = toDate, BranchId = branchId }));

    [HttpGet("equipment-utilization")]
    [ProducesResponseType(typeof(EquipmentUtilizationReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentUtilizationReportDto>> GetEquipmentUtilizationReport([FromQuery] Guid? branchId)
        => Ok(await _mediator.Send(new GetEquipmentUtilizationReportQuery { BranchId = branchId }));

    [HttpGet("branch-comparison")]
    [ProducesResponseType(typeof(BranchComparisonReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BranchComparisonReportDto>> GetBranchComparisonReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
        => Ok(await _mediator.Send(new GetBranchComparisonReportQuery { FromDate = fromDate, ToDate = toDate }));

    [HttpGet("commissions")]
    [ProducesResponseType(typeof(CommissionReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommissionReportDto>> GetCommissionReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? employeeId)
        => Ok(await _mediator.Send(new GetCommissionReportQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            EmployeeId = employeeId
        }));
}
