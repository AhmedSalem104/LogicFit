using LogicFit.Application.Features.Reports.DTOs;
using LogicFit.Application.Features.Reports.Queries.GetClientsReport;
using LogicFit.Application.Features.Reports.Queries.GetCoachDashboardReport;
using LogicFit.Application.Features.Reports.Queries.GetCoachTraineesReport;
using LogicFit.Application.Features.Reports.Queries.GetDashboardReport;
using LogicFit.Application.Features.Reports.Queries.GetFinancialReport;
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
}
