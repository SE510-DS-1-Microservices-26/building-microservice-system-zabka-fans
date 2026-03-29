namespace CoreService.Application.Exceptions;

public class ApplicationValidationException : Exception
{
    public string ErrorCode { get; }

    public ApplicationValidationException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}

