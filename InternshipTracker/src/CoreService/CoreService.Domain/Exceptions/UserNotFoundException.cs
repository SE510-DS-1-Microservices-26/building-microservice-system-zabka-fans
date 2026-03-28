namespace CoreService.Domain.Exceptions;

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(Guid userId)
        : base("User.NotFound", $"User with ID {userId} was not found.")
    {
    }
}

