using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Requests;
using UserService.Application.DTOs.Responses;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;

namespace UserService.Application.UseCases;

public class CreateUserUseCase : IUseCase<CreateUserRequest, UserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserDbMessagePublisher _publisher;
    private readonly ILogger<CreateUserUseCase> _logger;

    public CreateUserUseCase(IUserRepository userRepository, IUserDbMessagePublisher publisher,
        ILogger<CreateUserUseCase> logger)
    {
        _userRepository = userRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<UserResponse>> ExecuteAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = new User(Guid.NewGuid(), request.Name, request.Level);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        await _publisher.PublishUserCreatedAsync(
            user.Id, user.Name, user.Level.ToString(), cancellationToken);

        _logger.LogInformation("User {UserId} created: {Name}, level {Level}",
            user.Id, user.Name, user.Level);

        var response = new UserResponse(user.Id, user.Name, user.Level);
        return Result<UserResponse>.Success(response);
    }
}