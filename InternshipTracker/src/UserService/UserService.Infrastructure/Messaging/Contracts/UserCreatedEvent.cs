namespace Contracts.Users;

public record UserCreatedEvent(Guid Id, string Name, string Level);

