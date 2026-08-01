using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using LogicFit.Application.Common.Interfaces;
using LogicFit.Application.Features.Identity;
using LogicFit.Domain.Entities;
using LogicFit.Domain.Enums;
using LogicFit.Domain.Exceptions;
using LogicFit.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using EmailTokenGenerator = LogicFit.Application.Features.Identity.IdentityEmailActionToken;

namespace LogicFit.Infrastructure.Services;

/// <summary>Persistent, one-use WebAuthn ceremonies backed by Fido2.NetLib verification.</summary>
public sealed class IdentityPasskeyService : IIdentityPasskeyService
{
    private readonly IApplicationDbContext _context;
    private readonly IFido2 _fido2;
    private readonly IDateTimeService _clock;
    private readonly PasskeyOptions _options;

    public IdentityPasskeyService(IApplicationDbContext context, IFido2 fido2, IDateTimeService clock, IOptions<PasskeyOptions> options)
        => (_context, _fido2, _clock, _options) = (context, fido2, clock, options.Value);

    public async Task<PasskeyCeremonyOptionsDto> BeginRegistrationAsync(Guid identityAccountId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var identity = await VerifiedIdentityAsync(identityAccountId, cancellationToken);
        var existing = await _context.IdentityPasskeyCredentials
            .Where(x => x.IdentityAccountId == identity.Id && x.IsActive)
            .Select(x => new PublicKeyCredentialDescriptor(x.CredentialId)).ToListAsync(cancellationToken);
        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = ToFidoUser(identity),
            ExcludeCredentials = existing,
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Required
            },
            AttestationPreference = AttestationConveyancePreference.None,
            Extensions = new AuthenticationExtensionsClientInputs { CredProps = true }
        });
        return await StoreCeremonyAsync(identity.Id, IdentityPasskeyCeremonyPurpose.Registration, options.ToJson(), cancellationToken);
    }

    public async Task CompleteRegistrationAsync(Guid identityAccountId, Guid ceremonyId, JsonElement credential, string? friendlyName, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var ceremony = await GetCeremonyAsync(identityAccountId, ceremonyId, IdentityPasskeyCeremonyPurpose.Registration, cancellationToken);
        var response = Deserialize<AuthenticatorAttestationRawResponse>(credential);
        var originalOptions = CredentialCreateOptions.FromJson(ceremony.OptionsJson);
        var verified = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = response,
            OriginalOptions = originalOptions,
            IsCredentialIdUniqueToUserCallback = async (args, ct) =>
                !await _context.IdentityPasskeyCredentials.AnyAsync(x => x.CredentialId.SequenceEqual(args.CredentialId), ct)
        }, cancellationToken);
        ceremony.UsedAt = _clock.UtcNow;
        _context.IdentityPasskeyCredentials.Add(new IdentityPasskeyCredential
        {
            IdentityAccountId = identityAccountId,
            CredentialId = verified.Id,
            PublicKey = verified.PublicKey,
            UserHandle = verified.User.Id,
            SignatureCounter = verified.SignCount,
            FriendlyName = string.IsNullOrWhiteSpace(friendlyName) ? null : friendlyName.Trim()
        });
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Type = "identity.passkey.registered",
            Payload = $"{{\"identityAccountId\":\"{identityAccountId}\"}}",
            OccurredAtUtc = _clock.UtcNow,
            IdempotencyKey = $"identity-passkey-ceremony:{ceremonyId}:registered"
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PasskeyCeremonyOptionsDto> BeginSignInAsync(string email, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var normalized = IdentityEmailAddress.Normalize(email);
        var identity = await _context.IdentityAccounts.SingleOrDefaultAsync(x => x.NormalizedEmail == normalized, cancellationToken);
        if (identity is null || !identity.IsActive || identity.EmailVerifiedAt is null)
            throw new UnauthorizedException("Invalid credentials");
        return await BeginAssertionAsync(identity, IdentityPasskeyCeremonyPurpose.SignIn, cancellationToken);
    }

    public async Task<Guid> CompleteSignInAsync(Guid ceremonyId, JsonElement credential, CancellationToken cancellationToken = default)
    {
        var ceremony = await _context.IdentityPasskeyCeremonies.Include(x => x.IdentityAccount)
            .SingleOrDefaultAsync(x => x.Id == ceremonyId && x.Purpose == IdentityPasskeyCeremonyPurpose.SignIn, cancellationToken)
            ?? throw new UnauthorizedException("Passkey ceremony is invalid or expired.");
        if (ceremony.UsedAt.HasValue || ceremony.ExpiresAt <= _clock.UtcNow || !ceremony.IdentityAccount.IsActive || ceremony.IdentityAccount.EmailVerifiedAt is null)
            throw new UnauthorizedException("Passkey ceremony is invalid or expired.");
        await VerifyAssertionAsync(ceremony, credential, cancellationToken);
        return ceremony.IdentityAccountId;
    }

    public async Task<PasskeyCeremonyOptionsDto> BeginStepUpAsync(Guid identityAccountId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return await BeginAssertionAsync(await VerifiedIdentityAsync(identityAccountId, cancellationToken), IdentityPasskeyCeremonyPurpose.StepUp, cancellationToken);
    }

    public async Task<string> CompleteStepUpAsync(Guid identityAccountId, Guid ceremonyId, JsonElement credential, CancellationToken cancellationToken = default)
    {
        var ceremony = await GetCeremonyAsync(identityAccountId, ceremonyId, IdentityPasskeyCeremonyPurpose.StepUp, cancellationToken);
        await VerifyAssertionAsync(ceremony, credential, cancellationToken);
        var rawToken = EmailTokenGenerator.CreateRaw();
        _context.IdentityPasskeyStepUpSessions.Add(new IdentityPasskeyStepUpSession
        {
            IdentityAccountId = identityAccountId,
            TokenHash = EmailTokenGenerator.Hash(rawToken),
            ExpiresAt = _clock.UtcNow.AddMinutes(5)
        });
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Type = "identity.passkey.step_up_completed",
            Payload = $"{{\"identityAccountId\":\"{identityAccountId}\"}}",
            OccurredAtUtc = _clock.UtcNow,
            IdempotencyKey = $"identity-passkey-ceremony:{ceremonyId}:step-up"
        });
        await _context.SaveChangesAsync(cancellationToken);
        return rawToken;
    }

    private async Task<PasskeyCeremonyOptionsDto> BeginAssertionAsync(IdentityAccount identity, IdentityPasskeyCeremonyPurpose purpose, CancellationToken cancellationToken)
    {
        var credentials = await _context.IdentityPasskeyCredentials
            .Where(x => x.IdentityAccountId == identity.Id && x.IsActive)
            .Select(x => new PublicKeyCredentialDescriptor(x.CredentialId)).ToListAsync(cancellationToken);
        if (credentials.Count == 0)
            throw new UnauthorizedException("Invalid credentials");
        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = credentials,
            UserVerification = UserVerificationRequirement.Required,
            Extensions = new AuthenticationExtensionsClientInputs { Extensions = true }
        });
        return await StoreCeremonyAsync(identity.Id, purpose, options.ToJson(), cancellationToken);
    }

    private async Task VerifyAssertionAsync(IdentityPasskeyCeremony ceremony, JsonElement credential, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        if (ceremony.UsedAt.HasValue || ceremony.ExpiresAt <= _clock.UtcNow)
            throw new UnauthorizedException("Passkey ceremony is invalid or expired.");
        var response = Deserialize<AuthenticatorAssertionRawResponse>(credential);
        var stored = await _context.IdentityPasskeyCredentials
            .SingleOrDefaultAsync(x => x.IdentityAccountId == ceremony.IdentityAccountId && x.IsActive &&
                x.CredentialId.SequenceEqual(response.RawId), cancellationToken)
            ?? throw new UnauthorizedException("Invalid credentials");
        var options = AssertionOptions.FromJson(ceremony.OptionsJson);
        var result = await _fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = response,
            OriginalOptions = options,
            StoredPublicKey = stored.PublicKey,
            StoredSignatureCounter = stored.SignatureCounter,
            IsUserHandleOwnerOfCredentialIdCallback = async (args, ct) =>
                args.UserHandle.SequenceEqual(stored.UserHandle) &&
                await _context.IdentityPasskeyCredentials.AnyAsync(x => x.IdentityAccountId == ceremony.IdentityAccountId &&
                    x.IsActive && x.CredentialId.SequenceEqual(args.CredentialId), ct)
        }, cancellationToken);
        ceremony.UsedAt = _clock.UtcNow;
        stored.SignatureCounter = result.SignCount;
        stored.LastUsedAt = _clock.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<PasskeyCeremonyOptionsDto> StoreCeremonyAsync(Guid identityAccountId, IdentityPasskeyCeremonyPurpose purpose, string optionsJson, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var outstanding = await _context.IdentityPasskeyCeremonies.Where(x => x.IdentityAccountId == identityAccountId && x.Purpose == purpose && x.UsedAt == null && x.ExpiresAt > now).ToListAsync(cancellationToken);
        foreach (var item in outstanding) item.UsedAt = now;
        var ceremony = new IdentityPasskeyCeremony { IdentityAccountId = identityAccountId, Purpose = purpose, OptionsJson = optionsJson, ExpiresAt = now.AddMinutes(5) };
        _context.IdentityPasskeyCeremonies.Add(ceremony);
        await _context.SaveChangesAsync(cancellationToken);
        using var document = JsonDocument.Parse(optionsJson);
        return new PasskeyCeremonyOptionsDto { CeremonyId = ceremony.Id, Options = document.RootElement.Clone() };
    }

    private async Task<IdentityPasskeyCeremony> GetCeremonyAsync(Guid identityId, Guid ceremonyId, IdentityPasskeyCeremonyPurpose purpose, CancellationToken ct)
    {
        var ceremony = await _context.IdentityPasskeyCeremonies.SingleOrDefaultAsync(x => x.Id == ceremonyId && x.IdentityAccountId == identityId && x.Purpose == purpose, ct)
            ?? throw new UnauthorizedException("Passkey ceremony is invalid or expired.");
        if (ceremony.UsedAt.HasValue || ceremony.ExpiresAt <= _clock.UtcNow)
            throw new UnauthorizedException("Passkey ceremony is invalid or expired.");
        return ceremony;
    }

    private async Task<IdentityAccount> VerifiedIdentityAsync(Guid identityId, CancellationToken ct)
    {
        var identity = await _context.IdentityAccounts.SingleOrDefaultAsync(x => x.Id == identityId, ct)
            ?? throw new UnauthorizedException("Identity session is invalid.");
        if (!identity.IsActive || identity.EmailVerifiedAt is null)
            throw new UnauthorizedException("A verified active identity is required.");
        return identity;
    }

    private static Fido2User ToFidoUser(IdentityAccount identity) => new()
    {
        Id = identity.Id.ToByteArray(), Name = identity.Email, DisplayName = identity.FullName
    };

    private static T Deserialize<T>(JsonElement element) => JsonSerializer.Deserialize<T>(element.GetRawText())
        ?? throw new DomainException("Invalid passkey response.");

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured)
            throw new ServiceUnavailableException("PASSKEYS_NOT_CONFIGURED", "Passkeys are temporarily unavailable.");
    }
}
