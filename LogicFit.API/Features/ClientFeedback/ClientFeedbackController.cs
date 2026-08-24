using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClientFeedbackEntity = LogicFit.Domain.Entities.ClientFeedback;

namespace LogicFit.API.Features.ClientFeedback;

/// <summary>
/// Tenant-safe member feedback: clients submit for themselves and gym staff review it.
/// </summary>
[ApiController]
[Route("api/member-feedback")]
public sealed class ClientFeedbackController : ControllerBase
{
    private static readonly string[] NoteTypes = ["general", "problem", "complaint", "suggestion", "feature_request"];
    private readonly IApplicationDbContext _db;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _clock;

    public ClientFeedbackController(IApplicationDbContext db, ITenantService tenantService, ICurrentUserService currentUser, IDateTimeService clock)
        => (_db, _tenantService, _currentUser, _clock) = (db, tenantService, currentUser, clock);

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ClientFeedbackDto>> Submit(SubmitClientFeedbackRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var clientId = ParseCurrentUserId();
        Validate(request);
        var client = await _db.Users.AsNoTracking().Include(x => x.Profile)
            .FirstOrDefaultAsync(x => x.Id == clientId && x.TenantId == tenantId && !x.IsDeleted && x.IsActive, cancellationToken);
        if (client is null || client.Role != UserRole.Client) return Forbid();

        var feedback = new ClientFeedbackEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ClientId = clientId,
            Rating = request.Rating, NoteType = request.NoteType.Trim().ToLowerInvariant(),
            Message = request.Message.Trim(), Status = "New", SubmittedAt = _clock.UtcNow
        };
        _db.ClientFeedback.Add(feedback);
        await _db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ToDto(feedback, client.Profile?.FullName ?? client.Email, client.Email));
    }

    [HttpGet]
    [Authorize(Policy = Permissions.ViewMembers)]
    [Authorize(Policy = WorkspaceCapabilities.GymExperience)]
    public async Task<ActionResult<ClientFeedbackListResponse>> List(
        [FromQuery] int? rating, [FromQuery] string? noteType, [FromQuery] string? status,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (rating is < 1 or > 5) return BadRequest(new { code = "INVALID_FEEDBACK_RATING", message = "التقييم يجب أن يكون من 1 إلى 5 نجوم." });
        if (!string.IsNullOrWhiteSpace(noteType) && !NoteTypes.Contains(noteType.Trim().ToLowerInvariant(), StringComparer.Ordinal))
            return BadRequest(new { code = "INVALID_FEEDBACK_TYPE", message = "نوع الملاحظة غير صالح." });

        page = Math.Clamp(page, 1, 100_000); pageSize = Math.Clamp(pageSize, 5, 100);
        var tenantId = _tenantService.GetCurrentTenantId();
        var query = _db.ClientFeedback.AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted);
        if (rating.HasValue) query = query.Where(x => x.Rating == rating.Value);
        if (!string.IsNullOrWhiteSpace(noteType)) query = query.Where(x => x.NoteType == noteType.Trim().ToLower());
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => (x.Client.Profile != null && x.Client.Profile.FullName != null && x.Client.Profile.FullName.Contains(term))
                || x.Client.Email.Contains(term) || (x.Client.PhoneNumber != null && x.Client.PhoneNumber.Contains(term)));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.SubmittedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ClientFeedbackDto(x.Id, x.ClientId,
                x.Client.Profile != null ? x.Client.Profile.FullName : x.Client.Email, x.Client.Email,
                x.Rating, x.NoteType, x.Message, x.Status, x.SubmittedAt, x.ReviewedAt))
            .ToListAsync(cancellationToken);
        return Ok(new ClientFeedbackListResponse(items, total, page, pageSize));
    }

    [HttpGet("summary")]
    [Authorize(Policy = Permissions.ViewMembers)]
    [Authorize(Policy = WorkspaceCapabilities.GymExperience)]
    public async Task<ActionResult<ClientFeedbackSummaryDto>> Summary(CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var rows = await _db.ClientFeedback.AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .Select(x => new { x.Rating, x.Status }).ToListAsync(cancellationToken);
        return Ok(new ClientFeedbackSummaryDto(rows.Count, rows.Count == 0 ? 0 : Math.Round(rows.Average(x => x.Rating), 2),
            rows.Count(x => x.Status == "New"), rows.Count(x => x.Status == "Resolved")));
    }

    [HttpPost("{id:guid}/review")]
    [Authorize(Policy = Permissions.ManageMembers)]
    [Authorize(Policy = WorkspaceCapabilities.GymExperience)]
    public async Task<ActionResult<ClientFeedbackDto>> Review(Guid id, ReviewClientFeedbackRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var feedback = await _db.ClientFeedback.Include(x => x.Client).ThenInclude(x => x.Profile)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);
        if (feedback is null) return NotFound(new { code = "FEEDBACK_NOT_FOUND", message = "التقييم غير موجود." });
        var nextStatus = string.IsNullOrWhiteSpace(request.Status) ? "Reviewed" : request.Status.Trim();
        if (nextStatus is not ("New" or "Reviewed" or "Resolved"))
            return BadRequest(new { code = "INVALID_FEEDBACK_STATUS", message = "حالة التقييم غير صالحة." });
        feedback.Status = nextStatus; feedback.ReviewedAt = _clock.UtcNow;
        feedback.ReviewedById = Guid.TryParse(_currentUser.UserId, out var reviewerId) ? reviewerId : null;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(feedback, feedback.Client.Profile?.FullName ?? feedback.Client.Email, feedback.Client.Email));
    }

    private Guid ParseCurrentUserId()
    {
        if (!Guid.TryParse(_currentUser.UserId, out var id) || id == Guid.Empty) throw new ForbiddenException("A client identity is required.");
        return id;
    }

    private static void Validate(SubmitClientFeedbackRequest request)
    {
        if (request.Rating is < 1 or > 5) throw new ValidationException(nameof(request.Rating), "التقييم يجب أن يكون من 1 إلى 5 نجوم.");
        if (!NoteTypes.Contains(request.NoteType.Trim().ToLowerInvariant(), StringComparer.Ordinal)) throw new ValidationException(nameof(request.NoteType), "نوع الملاحظة غير صالح.");
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Trim().Length < 3) throw new ValidationException(nameof(request.Message), "اكتب ملاحظتك قبل الإرسال.");
        if (request.Message.Trim().Length > 4000) throw new ValidationException(nameof(request.Message), "الحد الأقصى للملاحظة 4000 حرف.");
    }

    private static ClientFeedbackDto ToDto(ClientFeedbackEntity feedback, string? clientName, string? email)
        => new(feedback.Id, feedback.ClientId, clientName, email, feedback.Rating, feedback.NoteType, feedback.Message, feedback.Status, feedback.SubmittedAt, feedback.ReviewedAt);
}

public sealed class SubmitClientFeedbackRequest { public int Rating { get; set; } public string NoteType { get; set; } = "general"; public string Message { get; set; } = string.Empty; }
public sealed class ReviewClientFeedbackRequest { public string? Status { get; set; } }
public sealed record ClientFeedbackDto(Guid Id, Guid ClientId, string? ClientName, string? Email, int Rating, string NoteType, string Message, string Status, DateTime SubmittedAt, DateTime? ReviewedAt);
public sealed record ClientFeedbackListResponse(IReadOnlyList<ClientFeedbackDto> Items, int Total, int Page, int PageSize);
public sealed record ClientFeedbackSummaryDto(int Total, double AverageRating, int NewCount, int ResolvedCount);
