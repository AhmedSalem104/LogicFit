using System.Text.Json;

namespace LogicFit.Application.Common.Interfaces;

public interface IIdentityPasskeyService
{
    Task<PasskeyCeremonyOptionsDto> BeginRegistrationAsync(Guid identityAccountId, CancellationToken cancellationToken = default);
    Task CompleteRegistrationAsync(Guid identityAccountId, Guid ceremonyId, JsonElement credential, string? friendlyName, CancellationToken cancellationToken = default);
    Task<PasskeyCeremonyOptionsDto> BeginSignInAsync(string email, CancellationToken cancellationToken = default);
    Task<Guid> CompleteSignInAsync(Guid ceremonyId, JsonElement credential, CancellationToken cancellationToken = default);
    Task<PasskeyCeremonyOptionsDto> BeginStepUpAsync(Guid identityAccountId, CancellationToken cancellationToken = default);
    Task<string> CompleteStepUpAsync(Guid identityAccountId, Guid ceremonyId, JsonElement credential, CancellationToken cancellationToken = default);
}

public sealed class PasskeyCeremonyOptionsDto
{
    public Guid CeremonyId { get; init; }
    public JsonElement Options { get; init; }
}
