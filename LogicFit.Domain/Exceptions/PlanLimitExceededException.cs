namespace LogicFit.Domain.Exceptions;

/// <summary>Stable quota failure returned at both request submission and Platform approval.</summary>
public sealed class PlanLimitExceededException : DomainException
{
    public PlanLimitExceededException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}
