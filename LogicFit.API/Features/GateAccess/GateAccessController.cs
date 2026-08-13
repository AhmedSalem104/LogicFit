using LogicFit.Application.Features.GateAccess.Commands.GateCheckInByQr;
using LogicFit.Application.Features.GateAccess.DTOs;
using LogicFit.Application.Features.GateAccess.Queries.GetGateAccessLogs;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace LogicFit.API.Features.GateAccess;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageAttendance)]
[Authorize(Policy = WorkspaceCapabilities.GymGateAccess)]
public class GateAccessController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public GateAccessController(IMediator mediator, IApplicationDbContext context, ITenantService tenantService)
    {
        _mediator = mediator;
        _context = context;
        _tenantService = tenantService;
    }

    [HttpGet("scan")]
    [ProducesResponseType(typeof(QrMemberLookupDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<QrMemberLookupDto>> Scan([FromQuery] string qrCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(qrCode) || qrCode.Length > 200) return BadRequest("QR code is required.");
        var tenantId = _tenantService.GetCurrentTenantId();
        var card = await _context.MembershipCards
            .AsNoTracking()
            .Include(c => c.Client).ThenInclude(c => c.Profile)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.QrCode == qrCode.Trim(), cancellationToken);
        if (card is null) return NotFound("Membership card was not found.");

        var now = DateTime.UtcNow;
        var subscription = await _context.ClientSubscriptions
            .AsNoTracking().Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId && s.ClientId == card.ClientId && !s.IsDeleted)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new QrMemberLookupDto
        {
            ClientId = card.ClientId,
            ClientName = card.Client.Profile?.FullName ?? card.Client.Email,
            Email = card.Client.Email,
            PhoneNumber = card.Client.PhoneNumber,
            ProfilePictureUrl = card.Client.Profile?.ProfilePictureUrl,
            MembershipCardId = card.Id,
            CardNumber = card.CardNumber,
            CardActive = card.IsActive && (!card.ExpiresAt.HasValue || card.ExpiresAt.Value >= now),
            CardExpiresAt = card.ExpiresAt,
            SubscriptionActive = subscription is not null && subscription.Status == SubscriptionStatus.Active && subscription.StartDate <= now && subscription.EndDate >= now,
            SubscriptionStatus = subscription?.Status.ToString(),
            PlanName = subscription?.Plan?.Name,
            SubscriptionStartDate = subscription?.StartDate,
            SubscriptionEndDate = subscription?.EndDate,
            RemainingAmount = subscription is null ? null : subscription.TotalAmount - subscription.AmountPaid
        });
    }

    [HttpPost("check-in-qr")]
    [ProducesResponseType(typeof(GateCheckInResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GateCheckInResultDto>> CheckInByQr(GateCheckInByQrCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("logs")]
    [ProducesResponseType(typeof(List<GateAccessLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<GateAccessLogDto>>> GetLogs(
        [FromQuery] Guid? clientId,
        [FromQuery] Guid? branchId,
        [FromQuery] GateAccessResult? result,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int take = 200)
    {
        var logs = await _mediator.Send(new GetGateAccessLogsQuery
        {
            ClientId = clientId,
            BranchId = branchId,
            Result = result,
            FromDate = fromDate,
            ToDate = toDate,
            Take = take
        });
        return Ok(logs);
    }
}
