using MassTransit;

namespace CoreService.Infrastructure.Messaging.Contracts;

[EntityName("UserDeletedEvent")]
public record UserDeletedEvent(Guid UserId);

