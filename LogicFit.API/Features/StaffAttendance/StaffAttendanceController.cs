using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.StaffAttendance.DTOs;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.StaffAttendance;

[ApiController]
[Route("api/staff-attendance")]
[Authorize(Policy = Permissions.ManageAttendance)]
public sealed class StaffAttendanceController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenant;
    private readonly IDateTimeService _clock;

    public StaffAttendanceController(IApplicationDbContext context, ITenantService tenant, IDateTimeService clock)
    { _context = context; _tenant = tenant; _clock = clock; }

    [HttpGet]
    public async Task<ActionResult<List<StaffAttendanceDto>>> Get([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] Guid? branchId, [FromQuery] Guid? userId, CancellationToken ct)
    {
        var tenantId = _tenant.GetCurrentTenantId();
        var q = _context.StaffAttendances.Include(a => a.User).Where(a => a.TenantId == tenantId);
        if (fromDate.HasValue) q = q.Where(a => a.CheckInTime >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(a => a.CheckInTime <= toDate.Value);
        if (branchId.HasValue) q = q.Where(a => a.BranchId == branchId.Value);
        if (userId.HasValue) q = q.Where(a => a.UserId == userId.Value);
        var rows = await q.OrderByDescending(a => a.CheckInTime).Take(1000).ToListAsync(ct);
        return Ok(rows.Select(a => new StaffAttendanceDto
        {
            Id = a.Id, UserId = a.UserId, EmployeeProfileId = a.EmployeeProfileId,
            Name = a.User.Profile != null ? a.User.Profile.FullName : a.User.Email,
            PhoneNumber = a.User.PhoneNumber, Email = a.User.Email, BranchId = a.BranchId,
            CheckInTime = a.CheckInTime, CheckOutTime = a.CheckOutTime,
            DurationMinutes = a.CheckOutTime.HasValue ? (a.CheckOutTime.Value - a.CheckInTime).TotalMinutes : null,
            Method = a.Method
        }).ToList());
    }

    [HttpPost("toggle-qr")]
    public async Task<ActionResult<StaffAttendanceDto>> ToggleByQr([FromBody] ToggleStaffQrRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.QrCode) || request.QrCode.Length > 200) return BadRequest("QR code is required.");
        var tenantId = _tenant.GetCurrentTenantId();
        var employee = await _context.EmployeeProfiles.Include(e => e.User).Include(e => e.Branches)
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.QrCode == request.QrCode.Trim() && e.QrRevokedAt == null, ct)
            ;
        var coach = employee == null
            ? await _context.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.TenantId == tenantId && u.StaffQrCode == request.QrCode.Trim() && u.StaffQrRevokedAt == null && u.Role == UserRole.Coach, ct)
            : null;
        if (employee == null && coach == null) throw new NotFoundException("Staff QR", Guid.Empty);
        if ((employee?.TerminationDate.HasValue ?? false) || !(employee?.User.IsActive ?? coach!.IsActive) || (employee?.User.IsDeleted ?? coach!.IsDeleted))
            throw new DomainException("Staff member is inactive.");
        var userId = employee?.UserId ?? coach!.Id;
        var branchId = request.BranchId ?? employee?.Branches.FirstOrDefault(b => b.IsPrimary)?.BranchId ?? coach?.PrimaryBranchId;
        if (branchId.HasValue && employee != null && !employee.Branches.Any(b => b.BranchId == branchId.Value))
            throw new DomainException("Staff member is not assigned to this branch.");
        var now = _clock.UtcNow;
        var open = await _context.StaffAttendances.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.UserId == userId && a.CheckOutTime == null, ct);
        LogicFit.Domain.Entities.StaffAttendance attendance;
        if (open != null)
        {
            open.CheckOutTime = now;
            attendance = open;
        }
        else
        {
            attendance = new LogicFit.Domain.Entities.StaffAttendance { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, EmployeeProfileId = employee?.Id, BranchId = branchId, CheckInTime = now, Method = GateAccessMethod.Qr };
            _context.StaffAttendances.Add(attendance);
        }
        _context.GateAccessLogs.Add(new GateAccessLog { Id = Guid.NewGuid(), TenantId = tenantId, StaffUserId = userId, SubjectType = "Staff", BranchId = branchId, AccessTime = now, Result = GateAccessResult.Granted, Method = GateAccessMethod.Qr, DenyReason = GateDenyReason.None, ScannedCode = request.QrCode });
        await _context.SaveChangesAsync(ct);
        var staffUser = employee?.User ?? coach!;
        return Ok(new StaffAttendanceDto { Id = attendance.Id, UserId = userId, EmployeeProfileId = employee?.Id, Name = staffUser.Profile?.FullName ?? staffUser.Email, PhoneNumber = staffUser.PhoneNumber, Email = staffUser.Email, BranchId = branchId, CheckInTime = attendance.CheckInTime, CheckOutTime = attendance.CheckOutTime, DurationMinutes = attendance.CheckOutTime.HasValue ? (attendance.CheckOutTime.Value - attendance.CheckInTime).TotalMinutes : null, Method = attendance.Method });
    }

    [HttpPost("{id}/check-out")]
    public async Task<IActionResult> CheckOut(Guid id, CancellationToken ct)
    {
        var row = await _context.StaffAttendances.FirstOrDefaultAsync(a => a.Id == id && a.CheckOutTime == null, ct) ?? throw new NotFoundException("Staff attendance", id);
        row.CheckOutTime = _clock.UtcNow;
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed class ToggleStaffQrRequest
{
    public string QrCode { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
}
