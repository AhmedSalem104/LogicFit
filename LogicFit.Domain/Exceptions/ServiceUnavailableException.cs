namespace LogicFit.Domain.Exceptions;

/// <summary>Raised when a required secure delivery dependency has not been configured or is unavailable.</summary>
public sealed class ServiceUnavailableException : DomainException
{
    public string Code { get; }

    public ServiceUnavailableException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
