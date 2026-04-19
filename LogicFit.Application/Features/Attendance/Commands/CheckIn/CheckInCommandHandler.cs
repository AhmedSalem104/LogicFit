using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Application.Features.Attendance.Commands.CheckIn;

public class CheckInCommandHandler : IRequestHandler<CheckInCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;

    public CheckInCommandHandler(IApplicationDbContext context, ITenantService tenantService, IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Guid> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var now = _dateTimeService.UtcNow;

        var client = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.ClientId && u.TenantId == tenantId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Client", request.ClientId);

        var branchId = request.BranchId ?? await ResolveDefaultBranchIdAsync(tenantId, cancellationToken);

        Branch? branch = null;
        if (branchId.HasValue)
        {
            branch = await _context.Branches
                .Include(b => b.OperatingHours)
                .FirstOrDefaultAsync(b => b.Id == branchId.Value && b.TenantId == tenantId, cancellationToken);

            if (branch == null)
            {
                await LogDeniedAsync(request.ClientId, branchId, null, GateDenyReason.ClientNotFound, now, cancellationToken);
                throw new NotFoundException("Branch", branchId.Value);
            }

            if (!branch.IsActive)
            {
                await LogDeniedAsync(request.ClientId, branchId, null, GateDenyReason.BranchInactive, now, cancellationToken);
                throw new DomainException("Branch is inactive");
            }
        }

        var openCheckIn = await _context.Attendances
            .AnyAsync(a => a.ClientId == request.ClientId && a.TenantId == tenantId
                && a.CheckOutTime == null && !a.IsDeleted, cancellationToken);

        if (openCheckIn)
        {
            await LogDeniedAsync(request.ClientId, branchId, null, GateDenyReason.AlreadyCheckedIn, now, cancellationToken);
            throw new ConflictException("Client already has an open check-in. Please check out first.");
        }

        var activeSubscription = await _context.ClientSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.ClientId == request.ClientId
                && s.TenantId == tenantId
                && s.Status == SubscriptionStatus.Active
                && s.StartDate <= now
                && s.EndDate >= now
                && !s.IsDeleted)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSubscription == null)
        {
            await LogDeniedAsync(request.ClientId, branchId, null, GateDenyReason.NoActiveSubscription, now, cancellationToken);
            throw new DomainException("Client has no active subscription");
        }

        if (activeSubscription.Plan.SessionsPerWeek.HasValue && activeSubscription.Plan.SessionsPerWeek.Value > 0)
        {
            var weekStart = now.AddDays(-7);
            var weekAttendances = await _context.Attendances
                .CountAsync(a => a.ClientId == request.ClientId
                    && a.TenantId == tenantId
                    && a.CheckInTime >= weekStart
                    && !a.IsDeleted, cancellationToken);

            if (weekAttendances >= activeSubscription.Plan.SessionsPerWeek.Value)
            {
                await LogDeniedAsync(request.ClientId, branchId, null, GateDenyReason.SessionsPerWeekExceeded, now, cancellationToken);
                throw new DomainException($"Weekly sessions limit ({activeSubscription.Plan.SessionsPerWeek.Value}) has been reached");
            }
        }

        if (branch != null && branch.Capacity.HasValue && branch.Capacity.Value > 0)
        {
            var currentlyInside = await _context.Attendances
                .CountAsync(a => a.BranchId == branch.Id
                    && a.TenantId == tenantId
                    && a.CheckOutTime == null
                    && !a.IsDeleted, cancellationToken);

            if (currentlyInside >= branch.Capacity.Value)
            {
                await LogDeniedAsync(request.ClientId, branchId, null, GateDenyReason.BranchCapacityFull, now, cancellationToken);
                throw new DomainException($"Branch capacity ({branch.Capacity.Value}) is full");
            }
        }

        if (branch != null && !IsWithinOperatingHours(branch, now))
        {
            await LogDeniedAsync(request.ClientId, branchId, null, GateDenyReason.OutsideOperatingHours, now, cancellationToken);
            throw new DomainException("Branch is currently closed (outside operating hours)");
        }

        var attendance = new Domain.Entities.Attendance
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = request.ClientId,
            BranchId = branchId,
            SubscriptionId = activeSubscription.Id,
            CheckInTime = now,
            Notes = request.Notes
        };

        _context.Attendances.Add(attendance);

        _context.GateAccessLogs.Add(new GateAccessLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientId = request.ClientId,
            BranchId = branchId,
            AccessTime = now,
            Result = GateAccessResult.Granted,
            Method = GateAccessMethod.Manual,
            DenyReason = GateDenyReason.None
        });

        await _context.SaveChangesAsync(cancellationToken);

        return attendance.Id;
    }

    private async Task<Guid?> ResolveDefaultBranchIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var defaultBranch = await _context.Branches
            .Where(b => b.TenantId == tenantId && b.IsDefault && b.IsActive)
            .Select(b => (Guid?)b.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return defaultBranch;
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

    private async Task LogDeniedAsync(Guid clientId, Guid? branchId, Guid? cardId, GateDenyReason reason, DateTime now, CancellationToken cancellationToken)
    {
        _context.GateAccessLogs.Add(new GateAccessLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.GetCurrentTenantId(),
            ClientId = clientId,
            BranchId = branchId,
            MembershipCardId = cardId,
            AccessTime = now,
            Result = GateAccessResult.Denied,
            Method = GateAccessMethod.Manual,
            DenyReason = reason
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
