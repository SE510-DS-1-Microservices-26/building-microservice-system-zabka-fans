namespace UserService.Infrastructure.Messaging.Contracts;

public record UserCreatedEvent(Guid Id, string Name, string Level);

