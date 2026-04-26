namespace UserService.Domain.Exceptions;

public class InvalidEmailException : DomainException
{
    public InvalidEmailException(string email)
        : base("User.InvalidEmail", $"'{email}' is not a valid email address.")
    {
    }
}

