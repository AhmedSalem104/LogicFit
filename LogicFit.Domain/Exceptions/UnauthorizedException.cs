namespace LogicFit.Domain.Exceptions;

public class UnauthorizedException : DomainException
{
    public UnauthorizedException() : base("You are not authorized to perform this action.")
    {
    }

    public UnauthorizedException(string message) : base(message)
    {
    }

    public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
