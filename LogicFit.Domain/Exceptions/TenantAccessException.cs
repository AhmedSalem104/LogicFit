namespace LogicFit.Domain.Exceptions;

/// <summary>
/// A gym (tenant) is not allowed to be accessed right now (suspended, expired, cancelled, archived...).
/// Carries a machine-readable <see cref="Code"/> (e.g. TENANT_SUSPENDED) and the HTTP status to return,
/// so the frontend can react precisely instead of parsing a generic 403.
/// </summary>
public class TenantAccessException : DomainException
{
    public string Code { get; }
    public int StatusCode { get; }

    public TenantAccessException(string code, int statusCode, string? message = null)
        : base(message ?? code)
    {
        Code = code;
        StatusCode = statusCode;
    }
}
