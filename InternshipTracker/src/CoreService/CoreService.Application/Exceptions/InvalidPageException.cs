namespace CoreService.Application.Exceptions;

public class InvalidPageException : ApplicationValidationException
{
    public InvalidPageException()
        : base("Pagination.InvalidPage", "Page must be greater than or equal to 1.")
    {
    }
}

