using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.API.Features.DayPasses;

/// <summary>
/// Gym-only daily visitor passes. A pass is a tenant-owned sale and its payment is
/// written in the same SaveChanges operation so reports never see a completed pass
/// without its corresponding payment row.
/// </summary>
[ApiController]
[Route("api/day-passes")]
[Authorize(Policy = Permissions.ManageDayPasses)]
[Authorize(Policy = WorkspaceCapabilities.GymExperience)]
public sealed class DayPassesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _clock;

    public DayPassesController(
        IApplicationDbContext db,
        ITenantService tenantService,
        ICurrentUserService currentUser,
        IDateTimeService clock)
    {
        _db = db;
        _tenantService = tenantService;
        _currentUser = currentUser;
        _clock = clock;
    }

    [HttpGet("pricing")]
    public async Task<ActionResult<IReadOnlyList<DayPassTypeDto>>> GetPricing(CancellationToken cancellationToken)
    {
        await EnsureDefaultTypesAsync(cancellationToken);
        var tenantId = _tenantService.GetCurrentTenantId();
        var types = await _db.DayPassTypes
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToTypeDto(x))
            .ToListAsync(cancellationToken);

        return Ok(types);
    }

    [HttpPut("pricing")]
    public async Task<ActionResult<IReadOnlyList<DayPassTypeDto>>> UpdatePricing(
        UpdateDayPassPricingRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Types is null || request.Types.Count == 0)
            return BadRequest(new { code = "DAY_PASS_PRICING_REQUIRED", message = "أدخل أسعار الحصص قبل الحفظ." });

        var tenantId = _tenantService.GetCurrentTenantId();
        var codes = request.Types.Select(x => (x.Code ?? string.Empty).Trim()).ToList();
        if (codes.Any(string.IsNullOrWhiteSpace) || codes.Count != codes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            return BadRequest(new { code = "DAY_PASS_TYPE_INVALID", message = "أنواع الحصص يجب أن تكون صحيحة وغير مكررة." });

        var existing = await _db.DayPassTypes
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var input in request.Types)
        {
            var code = input.Code!.Trim();
            if (!existing.TryGetValue(code, out var type))
                return BadRequest(new { code = "DAY_PASS_TYPE_NOT_FOUND", message = "نوع الحصة غير موجود." });
            if (input.Price <= 0)
                return BadRequest(new { code = "DAY_PASS_PRICE_INVALID", message = "سعر الحصة يجب أن يكون أكبر من صفر." });

            type.Name = Required(input.Name, "اسم الحصة", 120);
            type.Price = decimal.Round(input.Price, 2);
            type.IsActive = input.IsActive;
            type.SortOrder = input.SortOrder;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetPricing(cancellationToken);
    }

    [HttpGet]
    public async Task<ActionResult<DayPassListResponse>> GetDayPasses(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? typeCode,
        [FromQuery] PaymentMethod? paymentMethod,
        [FromQuery] string? search,
        [FromQuery] bool includeVoided = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        page = Math.Clamp(page, 1, 100_000);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var range = NormalizeRange(from, to);

        var query = _db.DayPassSales
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.VisitDate >= range.From && x.VisitDate <= range.To);

        if (!includeVoided)
            query = query.Where(x => x.Status == DayPassStatus.Completed);
        if (!string.IsNullOrWhiteSpace(typeCode))
            query = query.Where(x => x.PassTypeCodeSnapshot == typeCode.Trim());
        if (paymentMethod.HasValue)
            query = query.Where(x => x.PaymentMethod == paymentMethod.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.VisitorName.Contains(term)
                || (x.VisitorPhone != null && x.VisitorPhone.Contains(term))
                || x.ReferenceNumber.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.VisitDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDtoExpression())
            .ToListAsync(cancellationToken);

        return Ok(new DayPassListResponse(items, total, page, pageSize, range.From, range.To));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DayPassSummaryDto>> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var range = NormalizeRange(from, to);
        var rows = await _db.DayPassSales
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.Status == DayPassStatus.Completed
                && x.VisitDate >= range.From && x.VisitDate <= range.To)
            .Select(x => new { x.PassTypeCodeSnapshot, x.PassTypeNameSnapshot, x.AmountPaid })
            .ToListAsync(cancellationToken);

        var byType = rows
            .GroupBy(x => new { x.PassTypeCodeSnapshot, x.PassTypeNameSnapshot })
            .Select(group => new DayPassSummaryItemDto(
                group.Key.PassTypeCodeSnapshot,
                group.Key.PassTypeNameSnapshot,
                group.Count(),
                decimal.Round(group.Sum(x => x.AmountPaid), 2)))
            .OrderBy(x => x.Name)
            .ToList();

        return Ok(new DayPassSummaryDto(
            range.From,
            range.To,
            rows.Count,
            decimal.Round(rows.Sum(x => x.AmountPaid), 2),
            byType));
    }

    [HttpPost]
    public async Task<ActionResult<DayPassDto>> Create(
        CreateDayPassRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var typeCode = Required(request.TypeCode, "نوع الحصة", 40);
        var type = await _db.DayPassTypes
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && !x.IsDeleted && x.IsActive && x.Code == typeCode, cancellationToken);
        if (type is null)
            return BadRequest(new { code = "DAY_PASS_TYPE_UNAVAILABLE", message = "نوع الحصة غير متاح حالياً." });

        var visitorName = string.IsNullOrWhiteSpace(request.VisitorName) ? "زائر" : Required(request.VisitorName, "اسم الزائر", 120);
        var phone = NormalizePhone(request.VisitorPhone);
        var now = _clock.UtcNow;
        var visitDate = DateOnly(request.VisitDate ?? now);
        var reference = await GenerateReferenceAsync(tenantId, visitDate, cancellationToken);
        var method = request.PaymentMethod ?? PaymentMethod.Cash;
        var sale = new DayPassSale
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReferenceNumber = reference,
            VisitorName = visitorName,
            VisitorPhone = string.IsNullOrWhiteSpace(request.VisitorPhone) ? null : request.VisitorPhone.Trim(),
            VisitorPhoneNormalized = phone,
            DayPassTypeId = type.Id,
            PassTypeCodeSnapshot = type.Code,
            PassTypeNameSnapshot = type.Name,
            AmountDue = type.Price,
            AmountPaid = type.Price,
            PaymentMethod = method,
            VisitDate = visitDate,
            Notes = Optional(request.Notes, 500),
            Status = DayPassStatus.Completed
        };

        _db.DayPassSales.Add(sale);
        _db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DayPassSaleId = sale.Id,
            Amount = sale.AmountPaid,
            Method = method,
            ReceivedAt = now,
            ReceivedById = Guid.TryParse(_currentUser.UserId, out var userId) ? userId : null,
            ReceiptNumber = reference,
            ReferenceNumber = reference,
            Notes = sale.Notes
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Created($"/api/day-passes/{sale.Id}", ToDto(sale, null));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DayPassDto>> Update(
        Guid id,
        UpdateDayPassRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var sale = await _db.DayPassSales.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);
        if (sale is null) return NotFound(new { code = "DAY_PASS_NOT_FOUND", message = "الحصة غير موجودة." });
        if (sale.Status == DayPassStatus.Voided)
            return Conflict(new { code = "DAY_PASS_VOIDED", message = "لا يمكن تعديل حصة ملغاة." });

        var visitorName = string.IsNullOrWhiteSpace(request.VisitorName) ? "زائر" : Required(request.VisitorName, "اسم الزائر", 120);
        sale.VisitorName = visitorName;
        sale.VisitorPhone = string.IsNullOrWhiteSpace(request.VisitorPhone) ? null : request.VisitorPhone.Trim();
        sale.VisitorPhoneNormalized = NormalizePhone(request.VisitorPhone);
        sale.VisitDate = DateOnly(request.VisitDate ?? sale.VisitDate);
        sale.Notes = Optional(request.Notes, 500);
        if (request.PaymentMethod.HasValue)
            sale.PaymentMethod = request.PaymentMethod.Value;

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(sale, null));
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult> Void(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var sale = await _db.DayPassSales.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);
        if (sale is null) return NotFound(new { code = "DAY_PASS_NOT_FOUND", message = "الحصة غير موجودة." });
        if (sale.Status == DayPassStatus.Voided) return NoContent();
        sale.Status = DayPassStatus.Voided;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var sale = await _db.DayPassSales.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);
        if (sale is null) return NotFound(new { code = "DAY_PASS_NOT_FOUND", message = "الحصة غير موجودة." });

        var payment = await _db.Payments.FirstOrDefaultAsync(x => x.DayPassSaleId == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);
        if (payment is not null)
            _db.Payments.Remove(payment);
        _db.DayPassSales.Remove(sale);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/whatsapp-opened")]
    public async Task<ActionResult> MarkWhatsappOpened(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        var sale = await _db.DayPassSales.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);
        if (sale is null) return NotFound(new { code = "DAY_PASS_NOT_FOUND", message = "الحصة غير موجودة." });
        sale.WhatsappOpenedAt ??= _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task EnsureDefaultTypesAsync(CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetCurrentTenantId();
        if (await _db.DayPassTypes.AnyAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken)) return;

        _db.DayPassTypes.AddRange(
            new DayPassType { Id = Guid.NewGuid(), TenantId = tenantId, Code = "day_gym", Name = "حصة جيم فقط", Price = 30, SortOrder = 1 },
            new DayPassType { Id = Guid.NewGuid(), TenantId = tenantId, Code = "day_gym_cardio", Name = "حصة جيم وكارديو", Price = 40, SortOrder = 2 });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateReferenceAsync(Guid tenantId, DateTime visitDate, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var reference = $"DP-{visitDate:yyyyMMdd}-{suffix}";
            if (!await _db.DayPassSales.AnyAsync(x => x.TenantId == tenantId && x.ReferenceNumber == reference, cancellationToken))
                return reference;
        }

        throw new InvalidOperationException("Unable to generate a unique daily pass reference.");
    }

    private static DayPassDto ToDto(DayPassSale sale, string? message)
        => new(
            sale.Id,
            sale.TenantId,
            sale.ReferenceNumber,
            sale.VisitorName,
            sale.VisitorPhone,
            sale.PassTypeCodeSnapshot,
            sale.PassTypeNameSnapshot,
            sale.AmountDue,
            sale.AmountPaid,
            sale.PaymentMethod,
            sale.VisitDate,
            sale.Notes,
            sale.Status,
            sale.WhatsappOpenedAt,
            string.IsNullOrWhiteSpace(sale.VisitorPhoneNormalized) ? null : $"أهلاً {sale.VisitorName}! شكراً لحضورك اليوم. رقم الزيارة: {sale.ReferenceNumber}",
            message);

    private static DayPassTypeDto ToTypeDto(DayPassType type)
        => new(type.Id, type.Code, type.Name, type.Price, type.IsActive, type.SortOrder);

    private static DayPassDto ToDtoExpression(DayPassSale sale)
        => ToDto(sale, null);

    private static System.Linq.Expressions.Expression<Func<DayPassSale, DayPassDto>> ToDtoExpression()
        => sale => new DayPassDto(
            sale.Id,
            sale.TenantId,
            sale.ReferenceNumber,
            sale.VisitorName,
            sale.VisitorPhone,
            sale.PassTypeCodeSnapshot,
            sale.PassTypeNameSnapshot,
            sale.AmountDue,
            sale.AmountPaid,
            sale.PaymentMethod,
            sale.VisitDate,
            sale.Notes,
            sale.Status,
            sale.WhatsappOpenedAt,
            sale.VisitorPhoneNormalized == null ? null : "تم تجهيز رسالة الزيارة",
            null);

    private static string Required(string? value, string label, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0) throw new ValidationException(label, $"{label} مطلوب.");
        if (normalized.Length > maxLength) throw new ValidationException(label, $"{label} أطول من المسموح.");
        return normalized;
    }

    private static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ValidationException("Notes", "النص أطول من المسموح.");
        return normalized;
    }

    private static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal)) digits = digits[2..];
        if (digits.StartsWith("0", StringComparison.Ordinal) && digits.Length == 11) digits = $"20{digits[1..]}";
        if (digits.Length < 8 || digits.Length > 15) throw new ValidationException("VisitorPhone", "رقم هاتف الزائر غير صالح.");
        return digits;
    }

    private static DateTime DateOnly(DateTime value)
        => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private static (DateTime From, DateTime To) NormalizeRange(DateTime? from, DateTime? to)
    {
        var today = DateTime.UtcNow.Date;
        var end = DateOnly(to ?? today);
        var start = DateOnly(from ?? new DateTime(end.Year, end.Month, 1));
        if (start > end) throw new ValidationException("DateRange", "تاريخ البداية يجب أن يسبق تاريخ النهاية.");
        if ((end - start).TotalDays > 730) throw new ValidationException("DateRange", "أقصى فترة للعرض هي 730 يوماً.");
        return (start, end);
    }
}

public sealed record DayPassTypeDto(Guid Id, string Code, string Name, decimal Price, bool IsActive, int SortOrder);

public sealed record DayPassDto(
    Guid Id,
    Guid TenantId,
    string ReferenceNumber,
    string VisitorName,
    string? VisitorPhone,
    string PassTypeCode,
    string PassTypeName,
    decimal AmountDue,
    decimal AmountPaid,
    PaymentMethod PaymentMethod,
    DateTime VisitDate,
    string? Notes,
    DayPassStatus Status,
    DateTime? WhatsappOpenedAt,
    string? WhatsappMessage,
    string? Message);

public sealed record DayPassListResponse(
    IReadOnlyList<DayPassDto> Items,
    int Total,
    int Page,
    int PageSize,
    DateTime From,
    DateTime To);

public sealed record DayPassSummaryDto(
    DateTime From,
    DateTime To,
    int Count,
    decimal Amount,
    IReadOnlyList<DayPassSummaryItemDto> ByType);

public sealed record DayPassSummaryItemDto(string Code, string Name, int Count, decimal Amount);

public sealed class CreateDayPassRequest
{
    public string? VisitorName { get; set; }
    public string? VisitorPhone { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTime? VisitDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateDayPassRequest
{
    public string? VisitorName { get; set; }
    public string? VisitorPhone { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTime? VisitDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateDayPassPricingRequest
{
    public List<DayPassPricingInput> Types { get; set; } = new();
}

public sealed class DayPassPricingInput
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
