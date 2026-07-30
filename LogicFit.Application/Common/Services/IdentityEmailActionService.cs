using System.Net;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EmailActionTokenEntity = LogicFit.Domain.Entities.IdentityEmailActionToken;
using EmailTokenGenerator = LogicFit.Application.Features.Identity.IdentityEmailActionToken;

namespace LogicFit.Application.Common.Services;

/// <summary>
/// Issues and consumes email action links. Tokens are opaque, hashed at rest, and consumed through
/// a conditional SQL update so two concurrent requests cannot redeem the same link.
/// </summary>
public sealed class IdentityEmailActionService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailSender _emailSender;
    private readonly IIdentityEmailLinkFactory _linkFactory;

    public IdentityEmailActionService(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        ICurrentUserService currentUserService,
        IEmailSender emailSender,
        IIdentityEmailLinkFactory linkFactory)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _currentUserService = currentUserService;
        _emailSender = emailSender;
        _linkFactory = linkFactory;
    }

    public async Task IssueAsync(IdentityAccount identity, EmailActionTokenPurpose purpose, CancellationToken cancellationToken)
    {
        EnsureDeliveryAvailable();

        var now = _dateTimeService.UtcNow;
        var rawToken = EmailTokenGenerator.CreateRaw();
        var record = new EmailActionTokenEntity
        {
            IdentityAccountId = identity.Id,
            Purpose = purpose,
            TokenHash = EmailTokenGenerator.Hash(rawToken),
            ExpiresAt = now.AddMinutes(30),
            CreatedByIp = _currentUserService.IpAddress
        };

        var activeTokens = await _context.IdentityEmailActionTokens
            .Where(x => x.IdentityAccountId == identity.Id && x.Purpose == purpose &&
                        x.UsedAt == null && x.RevokedAt == null && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var activeToken in activeTokens)
            activeToken.RevokedAt = now;

        _context.IdentityEmailActionTokens.Add(record);
        await _context.SaveChangesAsync(cancellationToken);

        var link = purpose == EmailActionTokenPurpose.EmailVerification
            ? _linkFactory.CreateEmailVerificationLink(rawToken)
            : _linkFactory.CreatePasswordResetLink(rawToken);
        var encodedName = WebUtility.HtmlEncode(identity.FullName);
        var subject = purpose == EmailActionTokenPurpose.EmailVerification
            ? "Confirm your LogicFit email"
            : "Reset your LogicFit password";
        var action = purpose == EmailActionTokenPurpose.EmailVerification
            ? "confirm your email address"
            : "reset your password";
        var text = $"Hello {identity.FullName}, use this link to {action}: {link}\n\nThis link expires in 30 minutes and can only be used once.";
        var html = $"<p>Hello {encodedName},</p><p><a href=\"{WebUtility.HtmlEncode(link)}\">Click here to {action}</a>.</p><p>This link expires in 30 minutes and can only be used once.</p>";

        try
        {
            await _emailSender.SendAsync(new EmailMessage(identity.Email, subject, html, text), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Never include recipient, raw link, or provider diagnostics in either log or response.
            throw new ServiceUnavailableException(
                "IDENTITY_EMAIL_UNAVAILABLE",
                "Email verification is temporarily unavailable. Please try again later.");
        }
    }

    public void EnsureDeliveryAvailable()
    {
        if (!_emailSender.IsConfigured || !_linkFactory.IsConfigured)
            throw new ServiceUnavailableException(
                "IDENTITY_EMAIL_NOT_CONFIGURED",
                "Email verification is temporarily unavailable. Please try again later.");
    }

    public async Task<EmailActionTokenEntity> ConsumeAsync(
        string rawToken,
        EmailActionTokenPurpose purpose,
        CancellationToken cancellationToken)
    {
        var hash = EmailTokenGenerator.Hash(rawToken);
        var now = _dateTimeService.UtcNow;
        var token = await _context.IdentityEmailActionTokens
            .SingleOrDefaultAsync(x => x.TokenHash == hash && x.Purpose == purpose, cancellationToken);

        if (token is null || token.UsedAt is not null || token.RevokedAt is not null || token.ExpiresAt <= now)
            throw new DomainException("This email link is invalid, expired, or has already been used.");

        token.UsedAt = now;
        return token;
    }
}
