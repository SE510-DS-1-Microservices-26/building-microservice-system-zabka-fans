using InternshipTracker.Application.DTOs;
using InternshipTracker.Application.DTOs.Requests;
using InternshipTracker.Application.DTOs.Responses;
using InternshipTracker.Application.Enums;
using InternshipTracker.Application.Interfaces;
using InternshipTracker.Application.Interfaces.Repositories;
using InternshipTracker.Domain.Entities;

namespace InternshipTracker.Application.UseCases;

public class GetUserUseCase : IUseCase<GetUserRequest, GetUserResponse>
{
    private readonly IReadOnlyRepository<User> _userRepository;

    public GetUserUseCase(IReadOnlyRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<GetUserResponse>> ExecuteAsync(
        GetUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            if (user == null)
                return Result<GetUserResponse>.Failure(new Error(
                    "User.NotFound",
                    $"User with ID {request.UserId} was not found.",
                    ErrorType.NotFound));

            var response = new GetUserResponse(user.Id, user.Name, user.Level);

            return Result<GetUserResponse>.Success(response);
        }
        catch (Exception)
        {
            return Result<GetUserResponse>.Failure(new Error(
                "System.Failure",
                "An unexpected error occurred while fetching the user.",
                ErrorType.Failure));
        }
    }
}