using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Requests;
using UserService.Application.Enums;
using UserService.Application.Interfaces;

namespace UserService.Application.UseCases;

public class DeleteUserUseCase : IUseCase<DeleteUserRequest>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserDbMessagePublisher _publisher;
    private readonly ILogger<DeleteUserUseCase> _logger;

    public DeleteUserUseCase(IUserRepository userRepository, IUserDbMessagePublisher publisher,
        ILogger<DeleteUserUseCase> logger)
    {
        _userRepository = userRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result> ExecuteAsync(
        DeleteUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for deletion", request.UserId);
            return Result.Failure(new Error(
                "User.NotFound",
                $"User with ID {request.UserId} was not found.",
                ErrorType.NotFound));
        }

        _userRepository.Delete(user);

        // Publish BEFORE SaveChanges so the outbox message is included
        // in the same transaction as the delete (EF Outbox pattern).
        await _publisher.PublishUserDeletedAsync(user.Id, cancellationToken);

        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} deleted: {Name}", user.Id, user.Name);

        return Result.Success();
    }
}

