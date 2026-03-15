namespace InternshipTracker.Domain.Exceptions;

public class UnderqualifiedException : DomainException
{
    public UnderqualifiedException(string message) : base(message)
    {
    }
}