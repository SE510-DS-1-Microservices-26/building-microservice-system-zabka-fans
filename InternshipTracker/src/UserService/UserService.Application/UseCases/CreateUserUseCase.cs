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

    public CreateUserUseCase(IUserRepository userRepository, IUserDbMessagePublisher publisher)
    {
        _userRepository = userRepository;
        _publisher = publisher;
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

        var response = new UserResponse(user.Id, user.Name, user.Level);
        return Result<UserResponse>.Success(response);
    }
}