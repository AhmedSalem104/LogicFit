using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TenantEntity = LogicFit.Domain.Entities.Tenant;
using ClientFeedbackEntity = LogicFit.Domain.Entities.ClientFeedback;

namespace LogicFit.API.Features.MemberPortal;

/// <summary>
/// Read-only member portal compatible with the TOP GYM membership-code journey.
/// The workspace is resolved by TenantMiddleware; the opaque card code is the
/// second factor and is never returned in the report.
/// </summary>
[ApiController]
[Route("api/member-portal")]
[AllowAnonymous]
[EnableRateLimiting("member-public-portal")]
public sealed class MemberPortalController : ControllerBase
{
    private static readonly string[] FeedbackTypes =
        ["general", "problem", "complaint", "suggestion", "feature_request"];

    private readonly IApplicationDbContext _db;
    private readonly ITenantService _tenantService;
    private readonly IDateTimeService _clock;

    public MemberPortalController(
        IApplicationDbContext db,
        ITenantService tenantService,
        IDateTimeService clock)
        => (_db, _tenantService, _clock) = (db, tenantService, clock);

    [HttpPost("lookup")]
    [ProducesResponseType(typeof(MemberPortalReport), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberPortalReport>> Lookup(
        [FromBody] MemberPortalLookupRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await GetGymTenantAsync(cancellationToken);
        if (tenant is null)
            return NotFound(InvalidCodeResponse());

        var card = await FindActiveCardAsync(request.MembershipCode, _clock.UtcNow, cancellationToken);
        if (card is null)
            return NotFound(InvalidCodeResponse());

        var report = await BuildReportAsync(tenant, card, cancellationToken);
        return Ok(report);
    }

    [HttpPost("feedback")]
    [ProducesResponseType(typeof(MemberPortalFeedbackResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<MemberPortalFeedbackResponse>> SubmitFeedback(
        [FromBody] MemberPortalFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await GetGymTenantAsync(cancellationToken);
        if (tenant is null)
            return NotFound(InvalidCodeResponse());

        if (request.Rating is < 1 or > 5)
            return BadRequest(new { code = "INVALID_FEEDBACK_RATING", message = "التقييم يجب أن يكون من 1 إلى 5 نجوم." });

        var noteType = request.NoteType.Trim().ToLowerInvariant();
        if (!FeedbackTypes.Contains(noteType, StringComparer.Ordinal))
            return BadRequest(new { code = "INVALID_FEEDBACK_TYPE", message = "نوع الملاحظة غير صالح." });

        var message = request.Message.Trim();
        if (message.Length < 3 || message.Length > 4000)
            return BadRequest(new { code = "INVALID_FEEDBACK_MESSAGE", message = "اكتب ملاحظة من 3 إلى 4000 حرف." });

        var card = await FindActiveCardAsync(request.MembershipCode, _clock.UtcNow, cancellationToken);
        if (card is null)
            return NotFound(InvalidCodeResponse());

        var feedback = new ClientFeedbackEntity
        {
            Id = Guid.NewGuid(),
            TenantId = card.TenantId,
            ClientId = card.ClientId,
            Rating = request.Rating,
            NoteType = noteType,
            Message = message,
            Status = "New",
            SubmittedAt = _clock.UtcNow
        };

        _db.ClientFeedback.Add(feedback);
        await _db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new MemberPortalFeedbackResponse(
            feedback.Status,
            feedback.SubmittedAt,
            "تم استلام ملاحظتك، شكرًا لمشاركتها."));
    }

    private async Task<TenantEntity?> GetGymTenantAsync(CancellationToken cancellationToken)
    {
        if (_tenantService.CurrentTenantId is not { } tenantId || tenantId == Guid.Empty)
            return null;

        return await _db.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId && !x.IsDeleted && x.WorkspaceType == WorkspaceType.Gym)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<ActivePortalCard?> FindActiveCardAsync(
        string? membershipCode,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var code = NormalizeCode(membershipCode);
        if (code is null)
            return null;

        var tenantId = _tenantService.CurrentTenantId;
        if (tenantId is not { } currentTenantId)
            return null;

        return await _db.MembershipCards
            .AsNoTracking()
            .Where(card => card.TenantId == currentTenantId
                && card.IsActive
                && card.RevokedAt == null
                && (card.QrCode == code || card.CardNumber == code)
                && (!card.ExpiresAt.HasValue || card.ExpiresAt.Value > now))
            .Select(card => new ActivePortalCard(
                card.TenantId,
                card.ClientId,
                card.IssuedAt,
                card.ExpiresAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<MemberPortalReport> BuildReportAsync(
        TenantEntity tenant,
        ActivePortalCard card,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var client = await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == card.ClientId
                && user.TenantId == card.TenantId
                && !user.IsDeleted
                && user.IsActive
                && user.Role == UserRole.Client)
            .Select(user => new MemberPortalClient(
                user.Profile != null && user.Profile.FullName != null
                    ? user.Profile.FullName
                    : user.Email,
                user.Email,
                user.PhoneNumber))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The membership card is not linked to an active client.");

        var subscriptions = await _db.ClientSubscriptions
            .AsNoTracking()
            .Where(subscription => subscription.TenantId == card.TenantId
                && subscription.ClientId == card.ClientId
                && !subscription.IsDeleted)
            .OrderByDescending(subscription => subscription.EndDate)
            .Take(50)
            .Select(subscription => new SubscriptionSnapshot(
                subscription.Id,
                subscription.Plan.Name,
                subscription.StartDate,
                subscription.EndDate,
                subscription.Status,
                subscription.TotalAmount,
                subscription.AmountPaid,
                subscription.Discount))
            .ToListAsync(cancellationToken);

        var subscriptionIds = subscriptions.Select(x => x.Id).ToArray();
        var freezes = subscriptionIds.Length == 0
            ? []
            : await _db.SubscriptionFreezes
                .AsNoTracking()
                .Where(freeze => freeze.TenantId == card.TenantId
                    && subscriptionIds.Contains(freeze.SubscriptionId)
                    && !freeze.IsDeleted)
                .OrderByDescending(freeze => freeze.StartDate)
                .Select(freeze => new FreezeSnapshot(
                    freeze.SubscriptionId,
                    freeze.StartDate,
                    freeze.EndDate,
                    freeze.Reason,
                    freeze.IsActive))
                .ToListAsync(cancellationToken);

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(payment => payment.TenantId == card.TenantId
                && payment.ClientId == card.ClientId
                && !payment.IsDeleted)
            .OrderByDescending(payment => payment.ReceivedAt)
            .Take(100)
            .Select(payment => new MemberPortalPayment(
                payment.Amount,
                payment.Method,
                payment.ReceivedAt,
                payment.ReceiptNumber))
            .ToListAsync(cancellationToken);

        var attendance = await _db.Attendances
            .AsNoTracking()
            .Where(record => record.TenantId == card.TenantId
                && record.ClientId == card.ClientId
                && !record.IsDeleted)
            .OrderByDescending(record => record.CheckInTime)
            .Take(100)
            .Select(record => new MemberPortalAttendance(
                record.CheckInTime,
                record.CheckOutTime))
            .ToListAsync(cancellationToken);

        var mappedSubscriptions = subscriptions
            .Select(subscription => MapSubscription(subscription, freezes, now))
            .ToList();
        var current = mappedSubscriptions
            .OrderByDescending(subscription => subscription.IsCurrent)
            .ThenByDescending(subscription => subscription.EndDate)
            .FirstOrDefault();

        var expected = mappedSubscriptions.Sum(subscription => subscription.TotalAmount);
        var paid = mappedSubscriptions.Sum(subscription => subscription.AmountPaid);

        return new MemberPortalReport
        {
            Workspace = new MemberPortalWorkspace(
                tenant.Name,
                tenant.BrandingSettings?.AppName,
                tenant.BrandingSettings?.LogoUrl ?? tenant.LogoUrl,
                tenant.Subdomain,
                tenant.WorkspaceType.ToString()),
            Member = client,
            Card = new MemberPortalCard(card.IssuedAt, card.ExpiresAt),
            CurrentMembership = current,
            Memberships = mappedSubscriptions,
            Payments = payments,
            Attendance = attendance,
            Freezes = freezes
                .Select(freeze => new MemberPortalFreeze(
                    freeze.StartDate,
                    freeze.EndDate,
                    freeze.Reason,
                    freeze.IsActive))
                .ToList(),
            FinancialSummary = new MemberPortalFinancialSummary(
                expected,
                paid,
                Math.Max(0, expected - paid)),
            GeneratedAt = now
        };
    }

    private static MemberPortalMembership MapSubscription(
        SubscriptionSnapshot subscription,
        IReadOnlyCollection<FreezeSnapshot> freezes,
        DateTime now)
    {
        var activeFreeze = freezes.FirstOrDefault(freeze => freeze.SubscriptionId == subscription.Id
            && freeze.IsActive
            && freeze.StartDate <= now
            && freeze.EndDate >= now);

        var isCurrent = subscription.Status is SubscriptionStatus.Active or SubscriptionStatus.Trial
            && subscription.EndDate >= now;

        var status = activeFreeze is not null
            ? "Frozen"
            : subscription.Status switch
            {
                SubscriptionStatus.Active => subscription.EndDate < now ? "Expired" : "Active",
                SubscriptionStatus.Trial => subscription.EndDate < now ? "Expired" : "Trial",
                SubscriptionStatus.Suspended => "Suspended",
                SubscriptionStatus.Cancelled => "Cancelled",
                SubscriptionStatus.Expired => "Expired",
                _ => subscription.Status.ToString()
            };

        return new MemberPortalMembership(
            subscription.PlanName,
            subscription.StartDate,
            subscription.EndDate,
            status,
            isCurrent,
            subscription.TotalAmount,
            subscription.AmountPaid,
            Math.Max(0, subscription.TotalAmount - subscription.AmountPaid),
            subscription.Discount);
    }

    private static string? NormalizeCode(string? code)
    {
        var value = code?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 120)
            return null;

        // Membership cards are generated from opaque alphanumeric tokens and a short
        // human-readable card number. Reject spaces/control characters before EF sees it.
        return value.Any(char.IsControl) || value.Any(char.IsWhiteSpace) ? null : value;
    }

    private static object InvalidCodeResponse()
        => new { code = "MEMBERSHIP_CODE_INVALID", message = "كود العضوية غير صالح أو انتهت صلاحيته." };

    private sealed record ActivePortalCard(Guid TenantId, Guid ClientId, DateTime IssuedAt, DateTime? ExpiresAt);
    private sealed record SubscriptionSnapshot(
        Guid Id,
        string PlanName,
        DateTime StartDate,
        DateTime EndDate,
        SubscriptionStatus Status,
        decimal TotalAmount,
        decimal AmountPaid,
        decimal Discount);
    private sealed record FreezeSnapshot(
        Guid SubscriptionId,
        DateTime StartDate,
        DateTime EndDate,
        string? Reason,
        bool IsActive);
}

public sealed class MemberPortalLookupRequest
{
    public string MembershipCode { get; set; } = string.Empty;
}

public sealed class MemberPortalFeedbackRequest
{
    public string MembershipCode { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string NoteType { get; set; } = "general";
    public string Message { get; set; } = string.Empty;
}

public sealed class MemberPortalReport
{
    public MemberPortalWorkspace Workspace { get; init; } = null!;
    public MemberPortalClient Member { get; init; } = null!;
    public MemberPortalCard Card { get; init; } = null!;
    public MemberPortalMembership? CurrentMembership { get; init; }
    public IReadOnlyList<MemberPortalMembership> Memberships { get; init; } = [];
    public IReadOnlyList<MemberPortalPayment> Payments { get; init; } = [];
    public IReadOnlyList<MemberPortalAttendance> Attendance { get; init; } = [];
    public IReadOnlyList<MemberPortalFreeze> Freezes { get; init; } = [];
    public MemberPortalFinancialSummary FinancialSummary { get; init; } = null!;
    public DateTime GeneratedAt { get; init; }
}

public sealed record MemberPortalWorkspace(
    string Name,
    string? AppName,
    string? LogoUrl,
    string? Subdomain,
    string WorkspaceType);

public sealed record MemberPortalClient(
    string Name,
    string Email,
    string? PhoneNumber);

public sealed record MemberPortalCard(DateTime IssuedAt, DateTime? ExpiresAt);

public sealed record MemberPortalMembership(
    string PlanName,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    bool IsCurrent,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal RemainingAmount,
    decimal Discount);

public sealed record MemberPortalPayment(
    decimal Amount,
    PaymentMethod Method,
    DateTime ReceivedAt,
    string? ReceiptNumber);

public sealed record MemberPortalAttendance(
    DateTime CheckInTime,
    DateTime? CheckOutTime);

public sealed record MemberPortalFreeze(
    DateTime StartDate,
    DateTime EndDate,
    string? Reason,
    bool IsActive);

public sealed record MemberPortalFinancialSummary(
    decimal ExpectedAmount,
    decimal AmountPaid,
    decimal RemainingAmount);

public sealed record MemberPortalFeedbackResponse(
    string Status,
    DateTime SubmittedAt,
    string Message);
