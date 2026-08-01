using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity.DTOs;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LogicFit.Application.Common.Services;

public sealed class OtpService : IOtpService
{
    private readonly IApplicationDbContext _context;
    private readonly IOtpSender _sender;
    private readonly IDateTimeService _clock;
    private readonly ICurrentUserService _current;
    private readonly OtpOptions _options;

    public OtpService(IApplicationDbContext context, IOtpSender sender, IDateTimeService clock,
        ICurrentUserService current, IOptions<OtpOptions> options)
        => (_context, _sender, _clock, _current, _options) = (context, sender, clock, current, options.Value);

    public async Task<OtpChallengeDto> RequestAsync(string phoneNumber, OtpPurpose purpose,
        Guid? identityAccountId, string? sessionBinding, CancellationToken cancellationToken = default,
        bool sendToProvider = true)
    {
        var phone = NormalizePhone(phoneNumber);
        var now = _clock.UtcNow;
        var startOfDay = now.Date;
        var dailyCount = await _context.OtpChallenges.CountAsync(
            x => x.NormalizedPhoneNumber == phone && x.CreatedAtUtc >= startOfDay, cancellationToken);
        if (dailyCount >= _options.DailySendLimit)
            throw new ConflictException("OTP_DAILY_LIMIT_REACHED");

        var latest = await _context.OtpChallenges
            .Where(x => x.NormalizedPhoneNumber == phone && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is not null && latest.LastSentAtUtc.AddSeconds(_options.ResendCooldownSeconds) > now)
            throw new ConflictException("OTP_RESEND_COOLDOWN");

        var active = await _context.OtpChallenges
            .Where(x => x.NormalizedPhoneNumber == phone && x.Purpose == purpose &&
                        x.Status == OtpChallengeStatus.Pending && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var item in active)
        {
            item.Status = OtpChallengeStatus.Revoked;
            item.RevokedAtUtc = now;
        }

        var code = string.Equals(_options.Provider, "Development", StringComparison.OrdinalIgnoreCase)
            ? _options.DevelopmentFixedCode!
            : RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var saltBytes = RandomNumberGenerator.GetBytes(32);
        var salt = Convert.ToBase64String(saltBytes);
        var challenge = new OtpChallenge
        {
            IdentityAccountId = identityAccountId,
            NormalizedPhoneNumber = phone,
            Purpose = purpose,
            CodeSalt = salt,
            CodeHash = Hash(code, salt),
            ExpiresAtUtc = now.AddMinutes(_options.ExpiresInMinutes),
            MaxAttempts = _options.MaxAttempts,
            ResendCount = latest is null ? 0 : latest.ResendCount + 1,
            LastSentAtUtc = now,
            Provider = _options.Provider,
            CreatedAtUtc = now,
            SessionBinding = sessionBinding,
            DeliveryStatus = OtpDeliveryStatus.Queued
        };
        _context.OtpChallenges.Add(challenge);
        await _context.SaveChangesAsync(cancellationToken);

        if (!sendToProvider)
        {
            // Keep the public response indistinguishable without sending a paid message to an
            // unregistered number. The challenge remains real but cannot issue a session because
            // it is not bound to an identity account.
            challenge.Provider = "Suppressed";
            AddAudit("OtpSendSuppressed", challenge, true);
            await _context.SaveChangesAsync(cancellationToken);
            return ToDto(challenge, now);
        }

        try
        {
            var sent = await _sender.SendAsync(phone, code, purpose, cancellationToken);
            challenge.Provider = sent.Provider;
            challenge.ProviderMessageId = sent.ProviderMessageId;
            challenge.DeliveryStatus = sent.Status;
            await _context.SaveChangesAsync(cancellationToken);
            AddAudit("OtpSendSucceeded", challenge, true);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            challenge.Status = OtpChallengeStatus.Failed;
            challenge.DeliveryStatus = OtpDeliveryStatus.Failed;
            AddAudit("OtpSendFailed", challenge, false);
            await _context.SaveChangesAsync(CancellationToken.None);
            throw new ServiceUnavailableException("OTP_DELIVERY_FAILED", "OTP delivery is temporarily unavailable.");
        }

        return ToDto(challenge, now);
    }

    private OtpChallengeDto ToDto(OtpChallenge challenge, DateTime now) =>
        new()
        {
            ChallengeId = challenge.Id,
            Purpose = challenge.Purpose,
            ExpiresAtUtc = challenge.ExpiresAtUtc,
            ResendAvailableAtUtc = now.AddSeconds(_options.ResendCooldownSeconds),
            MaskedPhoneNumber = Mask(challenge.NormalizedPhoneNumber)
        };

    public async Task<OtpChallenge> VerifyAsync(Guid challengeId, string code, OtpPurpose purpose,
        string? sessionBinding, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        var now = _clock.UtcNow;
        try
        {
            var challenge = await _context.OtpChallenges.SingleOrDefaultAsync(x => x.Id == challengeId, cancellationToken)
                ?? throw new UnauthorizedException("OTP_INVALID");
            if (challenge.Purpose != purpose || challenge.Status != OtpChallengeStatus.Pending ||
                challenge.ConsumedAtUtc is not null || challenge.RevokedAtUtc is not null)
                throw new UnauthorizedException("OTP_INVALID");
            if (!string.Equals(challenge.SessionBinding, sessionBinding, StringComparison.Ordinal))
                throw new UnauthorizedException("OTP_SESSION_MISMATCH");
            if (challenge.ExpiresAtUtc <= now)
            {
                challenge.Status = OtpChallengeStatus.Expired;
                AddAudit("OtpVerifyExpired", challenge, false);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                throw new UnauthorizedException("OTP_EXPIRED");
            }
            if (challenge.AttemptCount >= challenge.MaxAttempts)
                throw new UnauthorizedException("OTP_LOCKED");

            challenge.AttemptCount++;
            var expected = Convert.FromBase64String(challenge.CodeHash);
            var supplied = Convert.FromBase64String(Hash(code, challenge.CodeSalt));
            if (!CryptographicOperations.FixedTimeEquals(expected, supplied))
            {
                if (challenge.AttemptCount >= challenge.MaxAttempts)
                    challenge.Status = OtpChallengeStatus.Locked;
                AddAudit("OtpVerifyFailed", challenge, false);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                throw new UnauthorizedException(challenge.Status == OtpChallengeStatus.Locked ? "OTP_LOCKED" : "OTP_INVALID");
            }

            challenge.Status = OtpChallengeStatus.Consumed;
            challenge.ConsumedAtUtc = now;
            var siblings = await _context.OtpChallenges
                .Where(x => x.Id != challenge.Id && x.NormalizedPhoneNumber == challenge.NormalizedPhoneNumber &&
                            x.Purpose == purpose && x.Status == OtpChallengeStatus.Pending)
                .ToListAsync(cancellationToken);
            foreach (var sibling in siblings)
            {
                sibling.Status = OtpChallengeStatus.Revoked;
                sibling.RevokedAtUtc = now;
            }
            AddAudit("OtpVerifySucceeded", challenge, true);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return challenge;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new UnauthorizedException("OTP_ALREADY_USED");
        }
    }

    public static string NormalizePhone(string value)
    {
        var input = value.Trim();
        if (System.Text.RegularExpressions.Regex.IsMatch(input, @"[^\d+\s()\-]"))
            throw new DomainException("PHONE_NUMBER_INVALID");
        var normalized = new string(input.Where(x => char.IsDigit(x) || x == '+').ToArray());
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^\+[1-9]\d{7,14}$"))
            throw new DomainException("PHONE_NUMBER_INVALID");
        return normalized;
    }

    private string Hash(string code, string salt)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.HmacSecret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{salt}:{code}")));
    }

    private void AddAudit(string eventName, OtpChallenge challenge, bool success)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = _current.UserId,
            Action = AuditAction.Create,
            EntityName = "SecurityAuthEvent",
            EntityId = challenge.Id.ToString(),
            NewValues = JsonSerializer.Serialize(new { Event = eventName, Success = success, Purpose = challenge.Purpose.ToString() }),
            Timestamp = _clock.UtcNow,
            IpAddress = _current.IpAddress,
            UserAgent = _current.UserAgent
        });
    }

    private static string Mask(string phone) => phone.Length <= 6 ? "***" : $"{phone[..3]}***{phone[^3..]}";
}
