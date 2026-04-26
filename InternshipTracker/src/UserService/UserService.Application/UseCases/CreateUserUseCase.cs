using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Requests;
using UserService.Application.DTOs.Responses;
using UserService.Application.Interfaces;
using UserService.Domain.Factories;

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
        var user = UserFactory.Create(Guid.NewGuid(), request.Name, request.Email, request.Level);

        await _userRepository.AddAsync(user, cancellationToken);

        // Got to publish before calling SaveChanges
        await _publisher.PublishUserCreatedAsync(
            user.Id, user.Name, user.Email, user.Level.ToString(), cancellationToken);
        
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} created: {Name}, level {Level}",
            user.Id, user.Name, user.Level);
        
        var response = new UserResponse(user.Id, user.Name, user.Email, user.Level);
        return Result<UserResponse>.Success(response);
    }
}