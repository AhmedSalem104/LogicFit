namespace LogicFit.Domain.Exceptions;

/// <summary>
/// A safe, machine-readable outcome for a workspace provisioning attempt. The identifiers are
/// operational references only; connection strings and provider exception messages never cross
/// the API boundary.
/// </summary>
public sealed class ProvisioningException : DomainException
{
    public ProvisioningException(
        string code,
        int statusCode,
        string message,
        bool retryable,
        Guid? tenantId = null,
        Guid? applicationRequestId = null,
        Guid? databaseResourceId = null,
        Exception? innerException = null)
        : base(message, innerException ?? new InvalidOperationException(message))
    {
        Code = code;
        StatusCode = statusCode;
        Retryable = retryable;
        TenantId = tenantId;
        ApplicationRequestId = applicationRequestId;
        DatabaseResourceId = databaseResourceId;
    }

    public string Code { get; }
    public int StatusCode { get; }
    public bool Retryable { get; }
    public Guid? TenantId { get; }
    public Guid? ApplicationRequestId { get; }
    public Guid? DatabaseResourceId { get; }
}
