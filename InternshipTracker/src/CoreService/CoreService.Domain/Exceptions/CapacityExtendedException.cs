namespace CoreService.Domain.Exceptions;

public class CapacityExceededException : DomainException
{
    public CapacityExceededException(string message) : base("Internship.CapacityExceeded", message)
    {
    }
}