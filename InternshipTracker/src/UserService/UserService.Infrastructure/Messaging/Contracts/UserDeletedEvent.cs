using MassTransit;

namespace UserService.Infrastructure.Messaging.Contracts;

[EntityName("UserDeletedEvent")]
public record UserDeletedEvent(Guid UserId);

