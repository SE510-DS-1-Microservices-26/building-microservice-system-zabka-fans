namespace InternshipTracker.Domain.Exceptions;

public class CapacityExceededException : DomainException
{
    public CapacityExceededException(string message) : base(message)
    {
    }
}