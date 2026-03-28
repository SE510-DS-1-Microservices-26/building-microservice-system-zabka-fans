using Contracts.Users;
using MassTransit;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Messaging;

public class UserDbMessagePublisher : IUserDbMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public UserDbMessagePublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishUserCreatedAsync(Guid id, string name, string level,
        CancellationToken cancellationToken = default)
    {
        await _publishEndpoint.Publish(new UserCreatedEvent(id, name, level), cancellationToken);
    }

    public async Task PublishUserDeletedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _publishEndpoint.Publish(new UserDeletedEvent(userId), cancellationToken);
    }
}

