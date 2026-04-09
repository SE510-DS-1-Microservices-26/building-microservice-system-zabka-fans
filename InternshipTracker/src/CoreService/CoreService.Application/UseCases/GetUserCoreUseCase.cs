using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Enums;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace CoreService.Application.UseCases;

public class GetUserCoreUseCase : IUseCase<GetUserRequest, UserCoreResponse>
{
    private readonly IUserCoreRepository _userCoreRepository;
    private readonly ILogger<GetUserCoreUseCase> _logger;

    public GetUserCoreUseCase(IUserCoreRepository userCoreRepository, ILogger<GetUserCoreUseCase> logger)
    {
        _userCoreRepository = userCoreRepository;
        _logger = logger;
    }

    public async Task<Result<UserCoreResponse>> ExecuteAsync(
        GetUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userCoreRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found in core database", request.UserId);
            return Result<UserCoreResponse>.Failure(new Error(
                "User.NotFound",
                $"User with ID {request.UserId} was not found.",
                ErrorType.NotFound));
        }

        var response = new UserCoreResponse(user.Id, user.Name, user.Email, user.Level);
        return Result<UserCoreResponse>.Success(response);
    }
}

