using MassTransit;

namespace UserService.Infrastructure.Messaging.Contracts;

[EntityName("UserCreatedEvent")]
public record UserCreatedEvent(Guid Id, string Name, string Level);

