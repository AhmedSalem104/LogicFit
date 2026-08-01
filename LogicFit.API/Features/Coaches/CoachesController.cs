using LogicFit.Application.Features.Coaches.Commands.CreateCoach;
using LogicFit.Application.Features.Coaches.Commands.DeleteCoach;
using LogicFit.Application.Features.Coaches.Commands.UpdateCoach;
using LogicFit.Application.Features.Coaches.DTOs;
using LogicFit.Application.Features.Coaches.Queries.GetCoachById;
using LogicFit.Application.Features.Coaches.Queries.GetCoaches;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Mvc;
using LogicFit.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using LogicFit.Domain.Exceptions;

namespace LogicFit.API.Features.Coaches;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.ManageCoaches)]
public class CoachesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenant;
    private readonly IDateTimeService _clock;

    public CoachesController(IMediator mediator, IApplicationDbContext context, ITenantService tenant, IDateTimeService clock)
    {
        _mediator = mediator;
        _context = context; _tenant = tenant; _clock = clock;
    }

    [HttpGet]
    public async Task<ActionResult<List<CoachDto>>> GetCoaches(
        [FromQuery] string? searchTerm,
        [FromQuery] bool? isActive)
    {
        var result = await _mediator.Send(new GetCoachesQuery
        {
            SearchTerm = searchTerm,
            IsActive = isActive
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CoachDto>> GetCoach(Guid id)
    {
        var result = await _mediator.Send(new GetCoachByIdQuery { Id = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateCoach(CreateCoachCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCoach(Guid id, UpdateCoachCommand command)
    {
        command.Id = id;
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCoach(Guid id)
    {
        await _mediator.Send(new DeleteCoachCommand { Id = id });
        return NoContent();
    }

    [HttpPost("{id}/qr/regenerate")]
    public async Task<ActionResult<object>> RegenerateQr(Guid id, CancellationToken ct)
    {
        var coach = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == _tenant.GetCurrentTenantId() && u.Role == LogicFit.Domain.Enums.UserRole.Coach, ct) ?? throw new NotFoundException("Coach", id);
        coach.StaffQrCode = $"staff:{coach.TenantId:N}:{Guid.NewGuid():N}"; coach.StaffQrGeneratedAt = _clock.UtcNow; coach.StaffQrRevokedAt = null;
        await _context.SaveChangesAsync(ct); return Ok(new { coach.Id, qrCode = coach.StaffQrCode, coach.StaffQrGeneratedAt });
    }

    [HttpPost("{id}/qr/revoke")]
    public async Task<IActionResult> RevokeQr(Guid id, CancellationToken ct)
    {
        var coach = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == _tenant.GetCurrentTenantId() && u.Role == LogicFit.Domain.Enums.UserRole.Coach, ct) ?? throw new NotFoundException("Coach", id);
        coach.StaffQrRevokedAt = _clock.UtcNow; await _context.SaveChangesAsync(ct); return NoContent();
    }
}
