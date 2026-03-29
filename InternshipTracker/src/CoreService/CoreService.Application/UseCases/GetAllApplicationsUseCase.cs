using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Exceptions;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace CoreService.Application.UseCases;

public class GetAllApplicationsUseCase : IUseCase<GetAllApplicationsRequest, PagedResult<ApplicationResponse>>
{
    private readonly IInternshipApplicationRepository _applicationRepository;
    private readonly ILogger<GetAllApplicationsUseCase> _logger;

    public GetAllApplicationsUseCase(IInternshipApplicationRepository applicationRepository,
        ILogger<GetAllApplicationsUseCase> logger)
    {
        _applicationRepository = applicationRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<ApplicationResponse>>> ExecuteAsync(
        GetAllApplicationsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Page < 1)
            throw new InvalidPageException();

        if (request.PageSize < 1)
            throw new InvalidPageSizeException();

        var pageSize = Math.Min(request.PageSize, 50);

        var (items, totalCount) =
            await _applicationRepository.GetPagedWithDetailsAsync(request.Page, pageSize, cancellationToken);

        _logger.LogInformation("Retrieved page {Page} of applications ({Count}/{Total})",
            request.Page, items.Count, totalCount);

        var responses = items.Select(a =>
            new ApplicationResponse(
                a.Id,
                a.CandidateId,
                a.Candidate.Name,
                a.CandidateLevel,
                a.Internship.Title,
                a.Status)).ToList();

        return Result<PagedResult<ApplicationResponse>>.Success(
            new PagedResult<ApplicationResponse>(responses, request.Page, pageSize, totalCount));
    }
}




