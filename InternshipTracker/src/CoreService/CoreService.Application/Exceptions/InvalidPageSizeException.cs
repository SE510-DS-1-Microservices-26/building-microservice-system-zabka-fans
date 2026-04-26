namespace CoreService.Application.Exceptions;

public class InvalidPageSizeException : ApplicationValidationException
{
    public InvalidPageSizeException()
        : base("Pagination.InvalidPageSize", "PageSize must be greater than or equal to 1.")
    {
    }
}

