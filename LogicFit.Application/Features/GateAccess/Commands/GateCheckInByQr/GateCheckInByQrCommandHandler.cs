using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.GateAccess.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.GateAccess.Commands.GateCheckInByQr;

public class GateCheckInByQrCommandHandler : IRequestHandler<GateCheckInByQrCommand, GateCheckInResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public GateCheckInByQrCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<GateCheckInResultDto> Handle(GateCheckInByQrCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;

        var card = await _context.MembershipCards
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.QrCode == request.QrCode, cancellationToken);

        if (card == null)
        {
            await LogAsync(null, request.BranchId, null, request.QrCode, GateAccessResult.Denied, GateDenyReason.ClientNotFound, now, cancellationToken);
            return new GateCheckInResultDto { Granted = false, Message = "Invalid QR code", DenyReason = GateDenyReason.ClientNotFound, BranchId = request.BranchId };
        }

        if (!card.IsActive)
        {
            await LogAsync(card.ClientId, request.BranchId, card.Id, request.QrCode, GateAccessResult.Denied, GateDenyReason.CardInactive, now, cancellationToken);
            return new GateCheckInResultDto { Granted = false, Message = "Card is inactive or revoked", DenyReason = GateDenyReason.CardInactive, ClientId = card.ClientId, BranchId = request.BranchId };
        }

        if (card.ExpiresAt.HasValue && card.ExpiresAt.Value < now)
        {
            await LogAsync(card.ClientId, request.BranchId, card.Id, request.QrCode, GateAccessResult.Denied, GateDenyReason.CardExpired, now, cancellationToken);
            return new GateCheckInResultDto { Granted = false, Message = "Card has expired", DenyReason = GateDenyReason.CardExpired, ClientId = card.ClientId, BranchId = request.BranchId };
        }

        var branchId = request.BranchId ?? await _context.Branches
            .Where(b => b.TenantId == tenantId && b.IsDefault && b.IsActive)
            .Select(b => (Guid?)b.Id)
            .FirstOrDefaultAsync(cancellationToken);

        Branch? branch = null;
        if (branchId.HasValue)
        {
            branch = await _context.Branches
                .Include(b => b.OperatingHours)
                .FirstOrDefaultAsync(b => b.Id == branchId.Value && b.TenantId == tenantId, cancellationToken);

            if (branch == null || !branch.IsActive)
            {
                await LogAsync(card.ClientId, branchId, card.Id, request.QrCode, GateAccessResult.Denied, GateDenyReason.BranchInactive, now, cancellationToken);
                return new GateCheckInResultDto { Granted = false, Message = "Branch is inactive", DenyReason = GateDenyReason.BranchInactive, ClientId = card.ClientId, BranchId = branchId };
            }
        }

        var openCheckIn = await _context.Attendances
            .AnyAsync(a => a.ClientId == card.ClientId && a.TenantId == tenantId && a.CheckOutTime == null && !a.IsDeleted, cancellationToken);

        if (openCheckIn)
        {
            await LogAsync(card.ClientId, branchId, card.Id, request.QrCode, GateAccessResult.Denied, GateDenyReason.AlreadyCheckedIn, now, cancellationToken);
            return new GateCheckInResultDto { Granted = false, Message = "Client is already checked in", DenyReason = GateDenyReason.AlreadyCheckedIn, ClientId = card.ClientId, BranchId = branchId };
        }

        var activeSubscription = await _context.ClientSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.ClientId == card.ClientId
                && s.TenantId == tenantId
                && s.Status == SubscriptionStatus.Active
                && s.StartDate <= now
                && s.EndDate >= now
                && !s.IsDeleted)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSubscription == null)
        {
            await LogAsync(card.ClientId, branchId, card.Id, request.QrCode, GateAccessResult.Denied, GateDenyReason.NoActiveSubscription, now, cancellationToken);
            return new GateCheckInResultDto { Granted = false, Message = "No active subscription", DenyReason = GateDenyReason.NoActiveSubscription, ClientId = card.ClientId, BranchId = branchId };
        }

        var isFrozen = await _context.SubscriptionFreezes
            .AnyAsync(f => f.SubscriptionId == activeSubscription.Id
                && f.TenantId == tenantId
                && f.IsActive
                && f.StartDate <= now
                && f.EndDate > now
                && !f.IsDeleted, cancellationToken);

        if (isFrozen)
        {
            await LogAsync(card.ClientId, branchId, card.Id, request.QrCode, GateAccessResult.Denied, GateDenyReason.SubscriptionFrozen, now, cancellationToken);
            return new GateCheckInResultDto { Granted = false, Message = "Subscription is currently frozen", DenyReason = GateDenyReason.SubscriptionFrozen, ClientId = card.ClientId, BranchId = branchId };
        }

        if (activeSubscription.Plan.SessionsPerWeek.HasValue && activeSubscription.Plan.SessionsPerWeek.Value > 0)
        {
            var weekStart = now.AddDays(-7);
            var weekAttendances = await _context.Attendances
                .CountAsync(a => a.ClientId == card.ClientId
                    && a.TenantId == tenantId
                    && a.CheckInTime >= weekStart
                    && !a.IsDeleted, cancellationToken);

            if (weekAttendances >= activeSubscription.Plan.SessionsPerWeek.Value)
            {
                await LogAsync(card.ClientId, branchId, card.Id, request.QrCode, GateAccessResult.Denied, GateDenyReason.SessionsPerWeekExceeded, now, cancellationToken);
                return new GateCheckInResultDto { Granted = false, Message = $"Weekly sessions limit reached ({activeSubscription.Plan.SessionsPerWeek.Value})", DenyReason = GateDenyReason.SessionsPerWeekExceeded, ClientId = card.ClientId, BranchId = branchId };
            }
        }

        if (branch != null && branch.Capacity.HasValue && branch.Capacity.Value > 0)
        {
            var currentlyInside = await _context.Attendances
                .CountAsync(a => a.BranchId == branch.Id && a.TenantId == tenantId && a.CheckOutTime == null && !a.IsDeleted, cancellationToken);

            if (currentlyInside >= branch.Capacity.Value)
            {
                await LogAsync(card.ClientId, branchId, card.Id, request.QrCode, GateAccessResult.Denied, GateDenyReason.BranchCapacityFull, now, cancellationToken);
                return new GateCheckInResultDto { Granted = false, Message = "Branch is at full capacity", DenyReason = GateDenyReason.BranchCapacityFull, ClientId = card.ClientId, BranchId = branchId };
            }
        }

        if (branch != null && !IsWithinOperatingHours(branch, now))
        {
            await LogAsync(card.ClientId, branchId, card.Id, request.QrCode, GateAccessResult.Denied, GateDenyReason.OutsideOperatingHours, now, cancellationToken);
            return new GateCheckInResultDto { Granted = false, Message = "Branch is closed (outside operating hours)", DenyReason = GateDenyReason.OutsideOperatingHours, ClientId = card.ClientId, BranchId = branchId };
        }

        var attendance = new Domain.Entities.Attendance
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = card.ClientId,
            BranchId = branchId,
            SubscriptionId = activeSubscription.Id,
            CheckInTime = now
        };

        _context.Attendances.Add(attendance);

        _context.GateAccessLogs.Add(new GateAccessLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = card.ClientId,
            BranchId = branchId,
            MembershipCardId = card.Id,
            AccessTime = now,
            Result = GateAccessResult.Granted,
            Method = GateAccessMethod.Qr,
            DenyReason = GateDenyReason.None,
            ScannedCode = request.QrCode
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new GateCheckInResultDto
        {
            Granted = true,
            Message = "Access granted",
            AttendanceId = attendance.Id,
            ClientId = card.ClientId,
            ClientName = card.Client.Email,
            BranchId = branchId
        };
    }

    private static bool IsWithinOperatingHours(Branch branch, DateTime now)
    {
        var todayHours = branch.OperatingHours.FirstOrDefault(h => h.DayOfWeek == now.DayOfWeek);
        if (todayHours != null)
        {
            if (todayHours.IsClosed) return false;
            var t = now.TimeOfDay;
            return t >= todayHours.OpenTime && t <= todayHours.CloseTime;
        }

        if (branch.OpenTime.HasValue && branch.CloseTime.HasValue)
        {
            var t = now.TimeOfDay;
            return t >= branch.OpenTime.Value && t <= branch.CloseTime.Value;
        }

        return true;
    }

    private async Task LogAsync(Guid? clientId, Guid? branchId, Guid? cardId, string? scannedCode, GateAccessResult result, GateDenyReason reason, DateTime now, CancellationToken cancellationToken)
    {
        _context.GateAccessLogs.Add(new GateAccessLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.GetCurrentTenantId(),
            ClientId = clientId,
            BranchId = branchId,
            MembershipCardId = cardId,
            AccessTime = now,
            Result = result,
            Method = GateAccessMethod.Qr,
            DenyReason = reason,
            ScannedCode = scannedCode
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
