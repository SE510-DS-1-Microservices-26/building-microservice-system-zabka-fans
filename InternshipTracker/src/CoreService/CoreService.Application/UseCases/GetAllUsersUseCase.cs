using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Exceptions;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace CoreService.Application.UseCases;

public class GetAllUsersUseCase : IUseCase<GetAllUsersRequest, PagedResult<UserCoreResponse>>
{
    private readonly IUserCoreRepository _userCoreRepository;
    private readonly ILogger<GetAllUsersUseCase> _logger;

    public GetAllUsersUseCase(IUserCoreRepository userCoreRepository, ILogger<GetAllUsersUseCase> logger)
    {
        _userCoreRepository = userCoreRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<UserCoreResponse>>> ExecuteAsync(
        GetAllUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Page < 1)
            throw new InvalidPageException();

        if (request.PageSize < 1)
            throw new InvalidPageSizeException();

        var pageSize = Math.Min(request.PageSize, 50);

        var (items, totalCount) = await _userCoreRepository.GetPagedAsync(request.Page, pageSize, cancellationToken);

        _logger.LogInformation("Retrieved page {Page} of users ({Count}/{Total})",
            request.Page, items.Count, totalCount);

        var responses = items
            .Select(u => new UserCoreResponse(u.Id, u.Name, u.Email, u.Level))
            .ToList();

        return Result<PagedResult<UserCoreResponse>>.Success(
            new PagedResult<UserCoreResponse>(responses, request.Page, pageSize, totalCount));
    }
}

