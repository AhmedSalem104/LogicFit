namespace LogicFit.Domain.Exceptions;

public class ForbiddenException : DomainException
{
    public ForbiddenException() : base("You do not have permission to access this resource.")
    {
    }

    public ForbiddenException(string message) : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
