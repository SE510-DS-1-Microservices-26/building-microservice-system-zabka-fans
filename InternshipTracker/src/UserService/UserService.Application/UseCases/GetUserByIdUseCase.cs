using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Requests;
using UserService.Application.DTOs.Responses;
using UserService.Application.Enums;
using UserService.Application.Interfaces;

namespace UserService.Application.UseCases;

public class GetUserByIdUseCase : IUseCase<GetUserRequest, UserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserByIdUseCase> _logger;

    public GetUserByIdUseCase(IUserRepository userRepository, ILogger<GetUserByIdUseCase> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserResponse>> ExecuteAsync(
        GetUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return Result<UserResponse>.Failure(new Error(
                "User.NotFound",
                $"User with ID {request.UserId} was not found.",
                ErrorType.NotFound));
        }

        var response = new UserResponse(user.Id, user.Name, user.Level);

        return Result<UserResponse>.Success(response);
    }
}