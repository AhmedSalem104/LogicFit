using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LogicFit.Infrastructure.Authorization;

/// <summary>Requires a passkey assertion completed in the preceding five minutes for destructive platform mutations.</summary>
public sealed class PasskeyStepUpRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "PasskeyStepUp";
    public const string HeaderName = "X-LogicFit-Step-Up";
}

public sealed class PasskeyStepUpHandler : AuthorizationHandler<PasskeyStepUpRequirement>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _clock;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PasskeyStepUpHandler(IApplicationDbContext context, IDateTimeService clock, IHttpContextAccessor httpContextAccessor)
        => (_context, _clock, _httpContextAccessor) = (context, clock, httpContextAccessor);

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PasskeyStepUpRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var rawToken = httpContext?.Request.Headers[PasskeyStepUpRequirement.HeaderName].ToString();
        var subject = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(rawToken) || !Guid.TryParse(subject, out var userId)) return;
        var identityId = await _context.Users.IgnoreQueryFilters()
            .Where(x => x.Id == userId && x.IsActive && !x.IsDeleted)
            .Select(x => x.IdentityAccountId)
            .SingleOrDefaultAsync();
        if (!identityId.HasValue) return;
        var isValid = await _context.IdentityPasskeyStepUpSessions
            .AnyAsync(x => x.IdentityAccountId == identityId.Value && x.TokenHash == IdentityEmailActionToken.Hash(rawToken) &&
                x.RevokedAt == null && x.ExpiresAt > _clock.UtcNow);
        if (isValid) context.Succeed(requirement);
    }
}
