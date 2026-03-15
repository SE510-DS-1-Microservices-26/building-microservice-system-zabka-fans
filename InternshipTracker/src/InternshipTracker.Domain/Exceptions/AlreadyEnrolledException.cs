namespace InternshipTracker.Domain.Exceptions;

public class AlreadyEnrolledException : DomainException
{
    public AlreadyEnrolledException(string message) : base(message)
    {
    }
}