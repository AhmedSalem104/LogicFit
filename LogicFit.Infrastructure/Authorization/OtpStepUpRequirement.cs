using System.Security.Claims;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Authorization;

public sealed class OtpStepUpRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "OtpStepUp";
    public const string HeaderName = "X-LogicFit-OTP-Step-Up";
}

public sealed class OtpStepUpHandler : AuthorizationHandler<OtpStepUpRequirement>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _clock;
    private readonly IHttpContextAccessor _http;

    public OtpStepUpHandler(IApplicationDbContext context, IDateTimeService clock, IHttpContextAccessor http)
        => (_context, _clock, _http) = (context, clock, http);

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OtpStepUpRequirement requirement)
    {
        var rawToken = _http.HttpContext?.Request.Headers[OtpStepUpRequirement.HeaderName].ToString();
        var binding = _http.HttpContext?.Request.Headers["X-Session-Id"].ToString();
        if (string.IsNullOrWhiteSpace(rawToken) ||
            !Guid.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return;
        var identityId = await _context.Users.IgnoreQueryFilters()
            .Where(x => x.Id == userId && x.IsActive && !x.IsDeleted)
            .Select(x => x.IdentityAccountId)
            .SingleOrDefaultAsync();
        if (!identityId.HasValue) return;
        var valid = await _context.OtpStepUpSessions.AnyAsync(x =>
            x.IdentityAccountId == identityId.Value &&
            x.TokenHash == IdentityEmailActionToken.Hash(rawToken) &&
            x.RevokedAtUtc == null && x.UsedAtUtc == null &&
            x.ExpiresAtUtc > _clock.UtcNow &&
            (x.SessionBinding == null || x.SessionBinding == binding));
        if (valid) context.Succeed(requirement);
    }
}
