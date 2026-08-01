using System.Net;
using System.Reflection;
using System.Security.Claims;
using LogicFit.API.Security;
using LogicFit.Application.Features.Platform.Auth.Commands.PlatformOtpLogin;
using LogicFit.Application.Features.Identity.Commands.Otp;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Common.Services;
using LogicFit.Application.Features.Auth.DTOs;
using LogicFit.Application.Features.Identity;
using LogicFit.Domain.Authorization;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Infrastructure;
using LogicFit.Infrastructure.Authorization;
using LogicFit.Infrastructure.Persistence;
using LogicFit.Infrastructure.Services;
using LogicFit.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace LogicFit.Tests;

public sealed class OtpSecurityTests
{
    [Fact]
    public async Task Development_otp_uses_1234_only_through_a_real_challenge()
    {
        await using var fixture = await OtpFixture.CreateAsync();
        var challenge = await fixture.Service.RequestAsync(
            "+201012345678", OtpPurpose.PasswordlessLogin, null, "device-1");

        Assert.Equal("1234", Assert.Single(fixture.Sender.Messages).Code);
        var stored = await fixture.Db.OtpChallenges.SingleAsync(x => x.Id == challenge.ChallengeId);
        Assert.NotEqual("1234", stored.CodeHash);
        Assert.DoesNotContain("1234", stored.CodeHash);
        Assert.Equal(OtpChallengeStatus.Pending, stored.Status);
    }

    [Fact]
    public async Task Enumeration_safe_request_creates_a_decoy_without_sending_to_an_unknown_phone()
    {
        await using var fixture = await OtpFixture.CreateAsync();
        var challenge = await fixture.Service.RequestAsync(
            "+201012345677", OtpPurpose.PasswordlessLogin, null, "device-unknown",
            sendToProvider: false);

        Assert.Empty(fixture.Sender.Messages);
        var stored = await fixture.Db.OtpChallenges.SingleAsync(x => x.Id == challenge.ChallengeId);
        Assert.Null(stored.IdentityAccountId);
        Assert.Equal("Suppressed", stored.Provider);
        Assert.Equal(OtpChallengeStatus.Pending, stored.Status);
    }

    [Fact]
    public async Task Wrong_code_is_rejected_and_attempts_lock_the_challenge()
    {
        await using var fixture = await OtpFixture.CreateAsync(maxAttempts: 2);
        var challenge = await fixture.Service.RequestAsync(
            "+201012345679", OtpPurpose.PhoneVerification, null, null);

        await Assert.ThrowsAsync<UnauthorizedException>(() => fixture.Service.VerifyAsync(
            challenge.ChallengeId, "9999", OtpPurpose.PhoneVerification, null));
        await Assert.ThrowsAsync<UnauthorizedException>(() => fixture.Service.VerifyAsync(
            challenge.ChallengeId, "9999", OtpPurpose.PhoneVerification, null));

        fixture.Db.ChangeTracker.Clear();
        Assert.Equal(OtpChallengeStatus.Locked,
            (await fixture.Db.OtpChallenges.SingleAsync(x => x.Id == challenge.ChallengeId)).Status);
    }

    [Fact]
    public async Task Otp_is_one_use_and_requires_the_exact_challenge_and_session()
    {
        await using var fixture = await OtpFixture.CreateAsync();
        var challenge = await fixture.Service.RequestAsync(
            "+201012345680", OtpPurpose.PasswordlessLogin, null, "browser-a");

        await Assert.ThrowsAsync<UnauthorizedException>(() => fixture.Service.VerifyAsync(
            challenge.ChallengeId, "1234", OtpPurpose.PasswordlessLogin, "browser-b"));
        await fixture.Service.VerifyAsync(challenge.ChallengeId, "1234", OtpPurpose.PasswordlessLogin, "browser-a");
        await Assert.ThrowsAsync<UnauthorizedException>(() => fixture.Service.VerifyAsync(
            challenge.ChallengeId, "1234", OtpPurpose.PasswordlessLogin, "browser-a"));
        await Assert.ThrowsAsync<UnauthorizedException>(() => fixture.Service.VerifyAsync(
            Guid.NewGuid(), "1234", OtpPurpose.PasswordlessLogin, "browser-a"));
    }

    [Fact]
    public async Task New_request_revokes_the_previous_code_after_cooldown()
    {
        await using var fixture = await OtpFixture.CreateAsync(cooldownSeconds: 0);
        var oldChallenge = await fixture.Service.RequestAsync(
            "+201012345681", OtpPurpose.ChangePhone, null, null);
        var current = await fixture.Service.RequestAsync(
            "+201012345681", OtpPurpose.ChangePhone, null, null);

        fixture.Db.ChangeTracker.Clear();
        Assert.Equal(OtpChallengeStatus.Revoked,
            (await fixture.Db.OtpChallenges.SingleAsync(x => x.Id == oldChallenge.ChallengeId)).Status);
        Assert.Equal(OtpChallengeStatus.Pending,
            (await fixture.Db.OtpChallenges.SingleAsync(x => x.Id == current.ChallengeId)).Status);
    }

    [Fact]
    public async Task Resend_cooldown_and_daily_phone_limit_are_enforced_in_the_database()
    {
        await using var cooldownFixture = await OtpFixture.CreateAsync(cooldownSeconds: 60);
        await cooldownFixture.Service.RequestAsync(
            "+201012345674", OtpPurpose.PhoneVerification, null, null);
        var cooldown = await Assert.ThrowsAsync<ConflictException>(() =>
            cooldownFixture.Service.RequestAsync(
                "+201012345674", OtpPurpose.PhoneVerification, null, null));
        Assert.Equal("OTP_RESEND_COOLDOWN", cooldown.Message);

        await using var dailyFixture = await OtpFixture.CreateAsync(cooldownSeconds: 0, dailyLimit: 1);
        await dailyFixture.Service.RequestAsync(
            "+201012345675", OtpPurpose.PhoneVerification, null, null);
        var daily = await Assert.ThrowsAsync<ConflictException>(() =>
            dailyFixture.Service.RequestAsync(
                "+201012345675", OtpPurpose.ChangePhone, null, null));
        Assert.Equal("OTP_DAILY_LIMIT_REACHED", daily.Message);
    }

    [Fact]
    public async Task Same_browser_recovers_the_pending_challenge_without_resending()
    {
        await using var fixture = await OtpFixture.CreateAsync(cooldownSeconds: 60);
        var original = await fixture.Service.RequestAsync(
            "+201012345670", OtpPurpose.PasswordlessLogin, null, "browser-a");

        var recovered = await fixture.Service.RequestAsync(
            "+201012345670", OtpPurpose.PasswordlessLogin, null, "browser-a");

        Assert.Equal(original.ChallengeId, recovered.ChallengeId);
        Assert.Equal(original.ExpiresAtUtc, recovered.ExpiresAtUtc);
        Assert.Equal(original.ResendAvailableAtUtc, recovered.ResendAvailableAtUtc);
        Assert.Single(fixture.Sender.Messages);
        Assert.Single(await fixture.Db.OtpChallenges.ToListAsync());

        var otherBrowser = await Assert.ThrowsAsync<ConflictException>(() => fixture.Service.RequestAsync(
            "+201012345670", OtpPurpose.PasswordlessLogin, null, "browser-b"));
        Assert.Equal("OTP_RESEND_COOLDOWN", otherBrowser.Message);
    }

    [Fact]
    public async Task Passwordless_otp_verifies_an_existing_unverified_phone_before_issuing_context()
    {
        await using var fixture = await OtpFixture.CreateAsync();
        var identity = new IdentityAccount
        {
            FullName = "Phone Login",
            Email = "phone-login@logicfit.test",
            NormalizedEmail = "PHONE-LOGIN@LOGICFIT.TEST",
            EmailVerifiedAt = fixture.Clock.UtcNow,
            PhoneNumber = "+201012345669",
            NormalizedPhoneNumber = "+201012345669",
            PasswordHash = "not-used",
            IsActive = true
        };
        fixture.Db.IdentityAccounts.Add(identity);
        await fixture.Db.SaveChangesAsync();
        var requestHandler = new RequestPhoneLoginOtpHandler(fixture.Db, fixture.Service);
        var challenge = await requestHandler.Handle(
            new RequestPhoneLoginOtpCommand(identity.NormalizedPhoneNumber, "browser-phone"),
            CancellationToken.None);
        var issuer = new RecordingIdentityWorkspaceSessionIssuer();
        var verifyHandler = new VerifyPhoneLoginOtpHandler(
            fixture.Db, fixture.Service, issuer, fixture.Clock);

        await verifyHandler.Handle(
            new VerifyPhoneLoginOtpCommand(challenge.ChallengeId, "1234", "browser-phone"),
            CancellationToken.None);

        fixture.Db.ChangeTracker.Clear();
        Assert.NotNull((await fixture.Db.IdentityAccounts.SingleAsync(x => x.Id == identity.Id)).PhoneVerifiedAt);
        Assert.Equal(identity.Id, issuer.IdentityAccountId);
    }

    [Fact]
    public async Task Expired_otp_is_rejected()
    {
        await using var fixture = await OtpFixture.CreateAsync();
        var challenge = await fixture.Service.RequestAsync(
            "+201012345682", OtpPurpose.PasswordReset, null, null);
        fixture.Clock.Advance(TimeSpan.FromMinutes(6));

        var error = await Assert.ThrowsAsync<UnauthorizedException>(() => fixture.Service.VerifyAsync(
            challenge.ChallengeId, "1234", OtpPurpose.PasswordReset, null));
        Assert.Equal("OTP_EXPIRED", error.Message);
    }

    [Fact]
    public async Task Concurrent_verification_consumes_a_challenge_exactly_once()
    {
        await using var fixture = await OtpFixture.CreateAsync();
        var challenge = await fixture.Service.RequestAsync(
            "+201012345683", OtpPurpose.SensitiveActionStepUp, null, "session-1");

        await using var firstDb = fixture.CreateContext();
        await using var secondDb = fixture.CreateContext();
        var firstService = fixture.CreateService(firstDb);
        var secondService = fixture.CreateService(secondDb);

        static async Task<bool> VerifyAsync(OtpService service, Guid challengeId)
        {
            try
            {
                await service.VerifyAsync(
                    challengeId, "1234", OtpPurpose.SensitiveActionStepUp, "session-1");
                return true;
            }
            catch (UnauthorizedException)
            {
                return false;
            }
        }

        var results = await Task.WhenAll(
            VerifyAsync(firstService, challenge.ChallengeId),
            VerifyAsync(secondService, challenge.ChallengeId));

        Assert.Single(results, x => x);
        fixture.Db.ChangeTracker.Clear();
        var stored = await fixture.Db.OtpChallenges.SingleAsync(x => x.Id == challenge.ChallengeId);
        Assert.Equal(OtpChallengeStatus.Consumed, stored.Status);
        Assert.NotNull(stored.ConsumedAtUtc);
    }

    [Fact]
    public async Task Refresh_rotation_detects_reuse_and_revokes_the_token_family()
    {
        await using var fixture = await OtpFixture.CreateAsync();
        var tenant = new Tenant
        {
            Name = "Refresh Test Workspace",
            Subdomain = $"refresh-{Guid.NewGuid():N}",
            Status = TenantStatus.Active
        };
        var user = new User
        {
            TenantId = tenant.Id,
            Email = $"refresh-{Guid.NewGuid():N}@logicfit.test",
            PasswordHash = "not-used-by-this-test",
            Role = UserRole.Owner,
            IsActive = true
        };
        fixture.Db.Tenants.Add(tenant);
        fixture.Db.Set<User>().Add(user);

        var service = new RefreshTokenService(fixture.Db, new FakeJwtService(), fixture.Clock);
        var original = service.Issue(user, "127.0.0.1", RefreshTokenService.SurfaceTenant);
        await fixture.Db.SaveChangesAsync();
        var originalValue = original.Token;

        var (_, replacement) = await service.RotateAsync(
            originalValue, "127.0.0.2", RefreshTokenService.SurfaceTenant);
        var replacementValue = replacement.Token;

        var error = await Assert.ThrowsAsync<UnauthorizedException>(() => service.RotateAsync(
            originalValue, "127.0.0.3", RefreshTokenService.SurfaceTenant));
        Assert.Equal("REFRESH_TOKEN_REUSE_DETECTED", error.Message);

        fixture.Db.ChangeTracker.Clear();
        var storedOriginal = await fixture.Db.RefreshTokens.SingleAsync(x => x.Token == originalValue);
        var storedReplacement = await fixture.Db.RefreshTokens.SingleAsync(x => x.Token == replacementValue);
        Assert.NotNull(storedOriginal.RevokedAt);
        Assert.NotNull(storedReplacement.RevokedAt);
    }

    [Fact]
    public async Task Platform_password_phase_issues_only_an_otp_challenge_and_no_session()
    {
        await using var fixture = await OtpFixture.CreateAsync();
        var identity = new IdentityAccount
        {
            FullName = "Platform Admin",
            Email = "admin@logicfit.test",
            NormalizedEmail = "ADMIN@LOGICFIT.TEST",
            EmailVerifiedAt = fixture.Clock.UtcNow,
            PhoneNumber = "+201012345672",
            NormalizedPhoneNumber = "+201012345672",
            PhoneVerifiedAt = fixture.Clock.UtcNow,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1")
        };
        var platformTenant = new Tenant
        {
            Id = PlatformConstants.PlatformTenantId,
            Name = "LogicFit Platform",
            Status = TenantStatus.Active
        };
        var user = new User
        {
            TenantId = platformTenant.Id,
            IdentityAccountId = identity.Id,
            Email = identity.Email,
            PhoneNumber = identity.PhoneNumber,
            PasswordHash = identity.PasswordHash,
            Role = UserRole.PlatformAdmin,
            IsActive = true
        };
        fixture.Db.Tenants.Add(platformTenant);
        fixture.Db.IdentityAccounts.Add(identity);
        fixture.Db.Set<User>().Add(user);
        await fixture.Db.SaveChangesAsync();

        var handler = new RequestPlatformLoginOtpHandler(
            fixture.Db, fixture.Service, fixture.Clock, fixture.CurrentUser);
        var challenge = await handler.Handle(
            new RequestPlatformLoginOtpCommand(identity.Email, "Password1", "platform-browser"),
            CancellationToken.None);

        Assert.Equal(OtpPurpose.PlatformAdminLogin, challenge.Purpose);
        Assert.Empty(await fixture.Db.RefreshTokens.ToListAsync());
        Assert.Empty(await fixture.Db.IdentityWorkspaceSessions.ToListAsync());
        Assert.Single(fixture.Sender.Messages);
        var auditPayloads = await fixture.Db.AuditLogs
            .Where(x => x.EntityName == "SecurityAuthEvent")
            .Select(x => x.NewValues ?? string.Empty)
            .ToListAsync();
        Assert.Contains(auditPayloads, value => value.Contains("PlatformPasswordLoginSucceeded", StringComparison.Ordinal));
        Assert.All(auditPayloads, value =>
        {
            Assert.DoesNotContain("Password1", value, StringComparison.Ordinal);
            Assert.DoesNotContain("1234", value, StringComparison.Ordinal);
            Assert.DoesNotContain(identity.Email, value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(identity.PhoneNumber!, value, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Sensitive_action_step_up_cannot_be_used_by_another_session()
    {
        await using var fixture = await OtpFixture.CreateAsync();
        var identity = new IdentityAccount
        {
            FullName = "Step Up Admin",
            Email = "stepup@logicfit.test",
            NormalizedEmail = "STEPUP@LOGICFIT.TEST",
            EmailVerifiedAt = fixture.Clock.UtcNow,
            PhoneNumber = "+201012345671",
            NormalizedPhoneNumber = "+201012345671",
            PhoneVerifiedAt = fixture.Clock.UtcNow,
            PasswordHash = "not-used"
        };
        var tenant = new Tenant { Name = "Step Up Tenant", Status = TenantStatus.Active };
        var user = new User
        {
            TenantId = tenant.Id,
            IdentityAccountId = identity.Id,
            Email = identity.Email,
            PasswordHash = "not-used",
            Role = UserRole.Owner,
            IsActive = true
        };
        fixture.Db.Tenants.Add(tenant);
        fixture.Db.IdentityAccounts.Add(identity);
        fixture.Db.Set<User>().Add(user);
        await fixture.Db.SaveChangesAsync();
        var challenge = await fixture.Service.RequestAsync(
            identity.NormalizedPhoneNumber!, OtpPurpose.SensitiveActionStepUp,
            identity.Id, "browser-a");
        await fixture.Service.VerifyAsync(
            challenge.ChallengeId, "1234", OtpPurpose.SensitiveActionStepUp, "browser-a");
        const string rawToken = "step-up-opaque-token";
        fixture.Db.OtpStepUpSessions.Add(new OtpStepUpSession
        {
            IdentityAccountId = identity.Id,
            OtpChallengeId = challenge.ChallengeId,
            TokenHash = LogicFit.Application.Features.Identity.IdentityEmailActionToken.Hash(rawToken),
            SessionBinding = "browser-a",
            ExpiresAtUtc = fixture.Clock.UtcNow.AddMinutes(5)
        });
        await fixture.Db.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }, "test"));
        var requirement = new OtpStepUpRequirement();
        var http = new DefaultHttpContext { User = principal };
        http.Request.Headers[OtpStepUpRequirement.HeaderName] = rawToken;
        http.Request.Headers["X-Session-Id"] = "browser-b";
        var handler = new OtpStepUpHandler(
            fixture.Db, fixture.Clock, new HttpContextAccessor { HttpContext = http });
        var wrongSession = new AuthorizationHandlerContext(
            new[] { requirement }, principal, null);
        await handler.HandleAsync(wrongSession);
        Assert.False(wrongSession.HasSucceeded);

        http.Request.Headers["X-Session-Id"] = "browser-a";
        var correctSession = new AuthorizationHandlerContext(
            new[] { requirement }, principal, null);
        await handler.HandleAsync(correctSession);
        Assert.True(correctSession.HasSucceeded);
    }

    [Fact]
    public void Development_provider_and_fixed_code_are_rejected_outside_development()
    {
        var productionWithDevelopmentProvider = Configuration("Production", "Development", "1234");
        var productionWithFixedCode = Configuration("Production", "MetaWhatsApp", "1234");

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddInfrastructure(productionWithDevelopmentProvider));
        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddInfrastructure(productionWithFixedCode));
    }

    [Fact]
    public void Development_provider_requires_the_reviewed_fixed_code()
    {
        var configuration = Configuration("Development", "Development", "9999");
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(configuration));
    }

    [Fact]
    public void Temporary_fixed_provider_requires_an_explicit_flag_reviewed_code_and_short_expiry()
    {
        var valid = TemporaryFixedConfiguration(true, "1234", DateTime.UtcNow.AddDays(7));
        new ServiceCollection().AddInfrastructure(valid);

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(
            TemporaryFixedConfiguration(false, "1234", DateTime.UtcNow.AddDays(7))));
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(
            TemporaryFixedConfiguration(true, "9999", DateTime.UtcNow.AddDays(7))));
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(
            TemporaryFixedConfiguration(true, "1234", DateTime.UtcNow.AddMinutes(-1))));
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(
            TemporaryFixedConfiguration(true, "1234", DateTime.UtcNow.AddDays(32))));
    }

    [Fact]
    public async Task Temporary_fixed_provider_uses_1234_only_until_its_runtime_expiry()
    {
        var expiresAt = new DateTime(2026, 7, 30, 12, 2, 0, DateTimeKind.Utc);
        await using var fixture = await OtpFixture.CreateAsync(
            cooldownSeconds: 0,
            provider: "TemporaryFixed",
            temporaryFixedExpiresAtUtc: expiresAt);

        var challenge = await fixture.Service.RequestAsync(
            "+201012345676", OtpPurpose.PlatformAdminLogin, null, "temporary-browser");
        Assert.Equal("1234", Assert.Single(fixture.Sender.Messages).Code);
        await fixture.Service.VerifyAsync(
            challenge.ChallengeId, "1234", OtpPurpose.PlatformAdminLogin, "temporary-browser");

        fixture.Clock.Advance(TimeSpan.FromMinutes(3));
        var error = await Assert.ThrowsAsync<ServiceUnavailableException>(() => fixture.Service.RequestAsync(
            "+201012345673", OtpPurpose.PlatformAdminLogin, null, "temporary-browser"));
        Assert.Equal("TEMPORARY_FIXED_OTP_EXPIRED", error.Code);
    }

    [Fact]
    public async Task Meta_provider_contract_is_mocked_and_never_calls_the_network()
    {
        var handler = new StubHttpHandler("""
            {"messaging_product":"whatsapp","messages":[{"id":"wamid.test"}]}
            """);
        var options = Options.Create(new MetaWhatsAppOptions
        {
            AccessToken = "test-secret-not-production",
            PhoneNumberId = "phone-id",
            BusinessAccountId = "business-id",
            TemplateName = "logicfit_otp",
            TemplateLanguage = "en_US",
            GraphApiVersion = "v21.0"
        });
        var provider = new MetaWhatsAppOtpProvider(new HttpClient(handler), options);

        var result = await provider.SendAsync("+201012345678", "654321",
            OtpPurpose.PasswordlessLogin, CancellationToken.None);

        Assert.Equal("wamid.test", result.ProviderMessageId);
        Assert.Equal(1, handler.CallCount);
        Assert.Contains("template", handler.Body);
    }

    [Fact]
    public void Refresh_token_is_json_hidden_and_cookie_is_hardened()
    {
        var property = typeof(AuthResponseDto).GetProperty(nameof(AuthResponseDto.RefreshToken))!;
        Assert.NotNull(property.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>());
        var context = new DefaultHttpContext();
        new RefreshTokenCookieManager().Write(context.Response, "opaque-token", RefreshTokenService.SurfaceTenant);
        var header = Assert.Single(context.Response.Headers["Set-Cookie"].ToArray()) ?? string.Empty;
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", header, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("opaque-token", System.Text.Json.JsonSerializer.Serialize(
            new AuthResponseDto { RefreshToken = "opaque-token" }));
    }

    [Fact]
    public void Runtime_types_and_routes_contain_no_passkey_or_webauthn_feature()
    {
        var assemblies = new[]
        {
            typeof(AuthResponseDto).Assembly,
            typeof(IdentityAccount).Assembly,
            typeof(DevelopmentOtpProvider).Assembly,
            typeof(RefreshTokenCookieManager).Assembly
        };
        var runtimeTypes = assemblies.SelectMany(x => x.GetTypes())
            .Where(x => x.Namespace?.Contains(".Persistence.Migrations", StringComparison.Ordinal) != true)
            .Select(x => x.FullName ?? x.Name).ToArray();
        Assert.DoesNotContain(runtimeTypes, x =>
            x.Contains("Passkey", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("WebAuthn", StringComparison.OrdinalIgnoreCase));
        var routes = typeof(RefreshTokenCookieManager).Assembly.GetTypes()
            .Where(x => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(x))
            .SelectMany(x => x.GetMethods())
            .SelectMany(x => x.GetCustomAttributes<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>())
            .Select(x => x.Template ?? string.Empty);
        Assert.DoesNotContain(routes, x => x.Contains("passkey", StringComparison.OrdinalIgnoreCase));
    }

    private static IConfiguration Configuration(string environment, string provider, string? fixedCode)
    {
        var values = new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = environment,
            ["Otp:Provider"] = provider,
            ["Otp:DevelopmentFixedCode"] = fixedCode,
            ["Otp:HmacSecret"] = "a-test-hmac-secret-that-is-longer-than-32-characters",
            ["JwtSettings:Secret"] = "a-test-jwt-secret-that-is-longer-than-32-characters",
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=otp-tests;"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static IConfiguration TemporaryFixedConfiguration(bool allow, string code, DateTime expiresAtUtc)
    {
        var values = new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["Otp:Provider"] = "TemporaryFixed",
            ["Otp:AllowTemporaryFixedCode"] = allow.ToString(),
            ["Otp:TemporaryFixedCode"] = code,
            ["Otp:TemporaryFixedCodeExpiresAtUtc"] = expiresAtUtc.ToString("O"),
            ["Otp:HmacSecret"] = "a-test-hmac-secret-that-is-longer-than-32-characters",
            ["JwtSettings:Secret"] = "a-test-jwt-secret-that-is-longer-than-32-characters",
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=otp-tests;"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class StubHttpHandler(string response) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public string Body { get; private set; } = string.Empty;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) };
        }
    }

    private sealed class OtpFixture : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; }
        public FakeOtpProvider Sender { get; }
        public MutableClock Clock { get; }
        public OtpService Service { get; }
        private string ConnectionString { get; }
        private FakeCurrentUser Current { get; }
        public ICurrentUserService CurrentUser => Current;
        private OtpOptions Options { get; }

        private OtpFixture(ApplicationDbContext db,
            FakeOtpProvider sender, MutableClock clock, OtpService service,
            string connectionString, FakeCurrentUser current, OtpOptions options)
            => (Db, Sender, Clock, Service, ConnectionString, Current, Options) =
                (db, sender, clock, service, connectionString, current, options);

        public static async Task<OtpFixture> CreateAsync(
            int maxAttempts = 5, int cooldownSeconds = 60, int dailyLimit = 10,
            string provider = "Development", DateTime? temporaryFixedExpiresAtUtc = null)
        {
            var databaseName = $"LogicFitOtpTests_{Guid.NewGuid():N}";
            var clock = new MutableClock();
            var current = new FakeCurrentUser();
            var baseConnectionString = Environment.GetEnvironmentVariable("LOGICFIT_TEST_CONNECTION_STRING")
                ?? "Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
            var connectionString = new SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = databaseName
            }.ConnectionString;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;
            var db = new ApplicationDbContext(options, new FakeTenantService(), current, clock);
            await db.Database.EnsureCreatedAsync();
            var sender = new FakeOtpProvider();
            var otpOptions = new OtpOptions
            {
                Provider = provider,
                DevelopmentFixedCode = provider == "Development" ? "1234" : null,
                AllowTemporaryFixedCode = provider == "TemporaryFixed",
                TemporaryFixedCode = provider == "TemporaryFixed" ? "1234" : null,
                TemporaryFixedCodeExpiresAtUtc = temporaryFixedExpiresAtUtc,
                HmacSecret = "a-test-hmac-secret-that-is-longer-than-32-characters",
                ExpiresInMinutes = 5,
                MaxAttempts = maxAttempts,
                ResendCooldownSeconds = cooldownSeconds,
                DailySendLimit = dailyLimit
            };
            var service = new OtpService(db, sender, clock, current, Microsoft.Extensions.Options.Options.Create(otpOptions));
            return new OtpFixture(db, sender, clock, service, connectionString, current, otpOptions);
        }

        public ApplicationDbContext CreateContext()
        {
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(ConnectionString).Options;
            return new ApplicationDbContext(dbOptions, new FakeTenantService(), Current, Clock);
        }

        public OtpService CreateService(ApplicationDbContext db) =>
            new(db, Sender, Clock, Current, Microsoft.Extensions.Options.Options.Create(Options));

        public async ValueTask DisposeAsync()
        {
            await Db.Database.EnsureDeletedAsync();
            await Db.DisposeAsync();
        }
    }

    private sealed class MutableClock : IDateTimeService
    {
        private DateTime _utcNow = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        public DateTime Now => _utcNow.ToLocalTime();
        public DateTime UtcNow => _utcNow;
        public void Advance(TimeSpan value) => _utcNow = _utcNow.Add(value);
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public string? UserId => null;
        public string? UserName => null;
        public Guid? TenantId => null;
        public bool IsAuthenticated => false;
        public string? IpAddress => "127.0.0.1";
        public string? UserAgent => "LogicFit.Tests";
    }

    private sealed class FakeTenantService : ITenantService
    {
        public Guid? CurrentTenantId => null;
        public Task SetTenantAsync(Guid tenantId) => Task.CompletedTask;
        public Task SetTenantBySubdomainAsync(string subdomain) => Task.CompletedTask;
        public Task<bool> SetTenantByCustomDomainAsync(string host) => Task.FromResult(false);
        public Task<bool> TenantExistsAsync(Guid tenantId) => Task.FromResult(false);
        public Task<Guid?> ResolveTenantIdAsync(string identifier) => Task.FromResult<Guid?>(null);
    }

    private sealed class FakeJwtService : IJwtService
    {
        public AccessTokenResult GenerateAccessToken(
            Guid userId, string email, Guid? tenantId, IEnumerable<string> roles,
            IEnumerable<string> permissions, int permissionVersion) =>
            new($"access-{Guid.NewGuid():N}", DateTime.UtcNow.AddMinutes(5));

        public string GenerateRefreshToken() => $"refresh-{Guid.NewGuid():N}";
    }

    private sealed class RecordingIdentityWorkspaceSessionIssuer : IIdentityWorkspaceSessionIssuer
    {
        public Guid? IdentityAccountId { get; private set; }

        public Task<LogicFit.Application.Features.Identity.DTOs.IdentitySignInDto> IssueAsync(
            Guid identityAccountId, CancellationToken cancellationToken = default)
        {
            IdentityAccountId = identityAccountId;
            return Task.FromResult(new LogicFit.Application.Features.Identity.DTOs.IdentitySignInDto());
        }
    }
}
