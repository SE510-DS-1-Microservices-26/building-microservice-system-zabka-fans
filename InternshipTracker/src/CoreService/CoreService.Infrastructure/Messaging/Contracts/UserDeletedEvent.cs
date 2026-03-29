using MassTransit;

namespace Contracts.Users;

[EntityName("UserDeletedEvent")]
public record UserDeletedEvent(Guid UserId);

