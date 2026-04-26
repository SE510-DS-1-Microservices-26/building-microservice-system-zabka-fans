namespace UserService.Application.Interfaces;

public interface IUserDbMessagePublisher
{
    Task PublishUserCreatedAsync(Guid id, string name, string email, string level, CancellationToken cancellationToken = default);
    Task PublishUserDeletedAsync(Guid userId, CancellationToken cancellationToken = default);
}