namespace CoreService.Domain.Exceptions;

public class DuplicateApplicationException : DomainException
{
    public DuplicateApplicationException(string message) : base("Application.Duplicate", message)
    {
    }
}