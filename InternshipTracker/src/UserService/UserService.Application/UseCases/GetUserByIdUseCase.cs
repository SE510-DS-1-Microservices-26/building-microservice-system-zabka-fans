using UserService.Application.DTOs;
using UserService.Application.DTOs.Requests;
using UserService.Application.DTOs.Responses;
using UserService.Application.Enums;
using UserService.Application.Interfaces;

namespace UserService.Application.UseCases;

public class GetUserByIdUseCase : IUseCase<GetUserRequest, UserResponse>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserResponse>> ExecuteAsync(
        GetUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            if (user == null)
                return Result<UserResponse>.Failure(new Error(
                    "User.NotFound",
                    $"User with ID {request.UserId} was not found.",
                    ErrorType.NotFound));

            var response = new UserResponse(user.Id, user.Name, user.Level);

            return Result<UserResponse>.Success(response);
        }
        catch (Exception)
        {
            return Result<UserResponse>.Failure(new Error(
                "System.Failure",
                "An unexpected error occurred while fetching the user.",
                ErrorType.Failure));
        }
    }
}