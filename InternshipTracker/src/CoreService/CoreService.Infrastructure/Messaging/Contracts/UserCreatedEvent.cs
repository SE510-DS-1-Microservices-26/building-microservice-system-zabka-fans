using MassTransit;

namespace Contracts.Users;

[EntityName("UserCreatedEvent")]
public record UserCreatedEvent(Guid Id, string Name, string Level);

