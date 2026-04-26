using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Exceptions;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace CoreService.Application.UseCases;

public class GetAllInternshipsUseCase : IUseCase<GetAllInternshipsRequest, PagedResult<InternshipResponse>>
{
    private readonly IInternshipRepository _internshipRepository;
    private readonly ILogger<GetAllInternshipsUseCase> _logger;

    public GetAllInternshipsUseCase(IInternshipRepository internshipRepository,
        ILogger<GetAllInternshipsUseCase> logger)
    {
        _internshipRepository = internshipRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<InternshipResponse>>> ExecuteAsync(
        GetAllInternshipsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Page < 1)
            throw new InvalidPageException();

        if (request.PageSize < 1)
            throw new InvalidPageSizeException();

        var pageSize = Math.Min(request.PageSize, 50);

        var (items, totalCount) = await _internshipRepository.GetPagedAsync(request.Page, pageSize, cancellationToken);

        _logger.LogInformation("Retrieved page {Page} of internships ({Count}/{Total})",
            request.Page, items.Count, totalCount);

        var responses = items.Select(internship =>
            new InternshipResponse(internship.Id, internship.Title, internship.Capacity, internship.MinimumLevel)).ToList();

        return Result<PagedResult<InternshipResponse>>.Success(
            new PagedResult<InternshipResponse>(responses, request.Page, pageSize, totalCount));
    }
}




