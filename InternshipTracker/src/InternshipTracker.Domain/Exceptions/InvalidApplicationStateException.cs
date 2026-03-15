namespace InternshipTracker.Domain.Exceptions;

public class InvalidApplicationStateException : DomainException
{
    public InvalidApplicationStateException(string message) : base(message) { }
}