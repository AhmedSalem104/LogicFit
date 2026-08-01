using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Domain.Enums;
using LogicFit.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LogicFit.API.Features.Identity;

[ApiController]
[Route("api/otp/webhooks/meta-whatsapp")]
[AllowAnonymous]
public sealed class MetaWhatsAppOtpWebhookController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly MetaWhatsAppOptions _options;

    public MetaWhatsAppOtpWebhookController(IApplicationDbContext db, IOptions<MetaWhatsAppOptions> options)
        => (_db, _options) = (db, options.Value);

    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (mode != "subscribe" || string.IsNullOrWhiteSpace(_options.WebhookVerifyToken) ||
            !FixedEquals(token, _options.WebhookVerifyToken))
            return Unauthorized();
        return Content(challenge ?? string.Empty, "text/plain");
    }

    [HttpPost]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AppSecret))
            return NotFound();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);
        if (!ValidSignature(body, Request.Headers["X-Hub-Signature-256"].ToString(), _options.AppSecret))
            return Unauthorized();

        using var document = JsonDocument.Parse(body);
        var updates = new List<(string Id, OtpDeliveryStatus Status)>();
        if (document.RootElement.TryGetProperty("entry", out var entries))
        {
            foreach (var entry in entries.EnumerateArray())
            foreach (var change in entry.GetProperty("changes").EnumerateArray())
            {
                if (!change.GetProperty("value").TryGetProperty("statuses", out var statuses)) continue;
                foreach (var status in statuses.EnumerateArray())
                {
                    var id = status.GetProperty("id").GetString();
                    var value = status.GetProperty("status").GetString();
                    if (!string.IsNullOrWhiteSpace(id))
                        updates.Add((id, Map(value)));
                }
            }
        }

        foreach (var update in updates)
        {
            var challenge = await _db.OtpChallenges.SingleOrDefaultAsync(
                x => x.ProviderMessageId == update.Id, cancellationToken);
            if (challenge is not null)
                challenge.DeliveryStatus = update.Status;
        }
        if (updates.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    private static OtpDeliveryStatus Map(string? value) => value switch
    {
        "sent" => OtpDeliveryStatus.Sent,
        "delivered" or "read" => OtpDeliveryStatus.Delivered,
        "failed" => OtpDeliveryStatus.Failed,
        _ => OtpDeliveryStatus.Queued
    };

    private static bool ValidSignature(string body, string signature, string secret)
    {
        if (!signature.StartsWith("sha256=", StringComparison.Ordinal)) return false;
        var supplied = signature["sha256=".Length..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        return FixedEquals(supplied, expected);
    }

    private static bool FixedEquals(string? left, string right)
    {
        if (left is null) return false;
        var a = Encoding.UTF8.GetBytes(left);
        var b = Encoding.UTF8.GetBytes(right);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
