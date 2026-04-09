using MassTransit;
using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces;
using UserService.Infrastructure.Messaging.Contracts;

namespace UserService.Infrastructure.Messaging;

public class UserDbMessagePublisher : IUserDbMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UserDbMessagePublisher> _logger;

    public UserDbMessagePublisher(IPublishEndpoint publishEndpoint, ILogger<UserDbMessagePublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishUserCreatedAsync(Guid id, string name, string email, string level,
        CancellationToken cancellationToken = default)
    {
        await _publishEndpoint.Publish(new UserCreatedEvent(id, name, email, level), cancellationToken);
        _logger.LogInformation("User created event with id {Id}", id);
    }

    public async Task PublishUserDeletedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _publishEndpoint.Publish(new UserDeletedEvent(userId), cancellationToken);
        _logger.LogInformation("User deleted event with id {Id}", userId);
    }
}

