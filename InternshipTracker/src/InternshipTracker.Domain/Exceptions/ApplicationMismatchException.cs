namespace InternshipTracker.Domain.Exceptions;

public class ApplicationMismatchException : DomainException
{
    public ApplicationMismatchException(string message) : base(message)
    {
    }
}