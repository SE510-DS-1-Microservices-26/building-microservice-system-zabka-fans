namespace CoreService.Domain.Exceptions;

public class InvalidApplicationStateException : DomainException
{
    public InvalidApplicationStateException(string message) : base("Application.InvalidState", message)
    {
    }
}