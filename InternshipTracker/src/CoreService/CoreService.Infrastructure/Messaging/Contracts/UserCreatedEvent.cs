using MassTransit;

namespace CoreService.Infrastructure.Messaging.Contracts;

[EntityName("UserCreatedEvent")]
public record UserCreatedEvent(Guid Id, string Name, string Email, string Level);

