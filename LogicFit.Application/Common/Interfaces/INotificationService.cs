namespace LogicFit.Application.Common.Interfaces;

/// <summary>
/// Sends templated notifications (in-app + email) to gym owners. Adds the in-app row to the current
/// unit of work (the caller commits it); email is dispatched via <see cref="IEmailService"/>.
/// </summary>
public interface INotificationService
{
    Task NotifyTenantOwnerAsync(
        Guid tenantId,
        string templateCode,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
