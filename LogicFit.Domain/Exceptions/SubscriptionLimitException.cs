namespace LogicFit.Domain.Exceptions;

/// <summary>
/// Thrown when an operation is blocked by the tenant's plan — a disabled feature or an exceeded
/// usage limit. Mapped to HTTP 402 (Payment Required).
/// </summary>
public class SubscriptionLimitException : DomainException
{
    public SubscriptionLimitException(string message) : base(message)
    {
    }
}
