using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Enums;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;

namespace CoreService.Application.UseCases;

public class GetInternshipUseCase : IUseCase<GetInternshipRequest, InternshipResponse>
{
    private readonly IInternshipRepository _internshipRepository;

    public GetInternshipUseCase(IInternshipRepository internshipRepository)
    {
        _internshipRepository = internshipRepository;
    }

    public async Task<Result<InternshipResponse>> ExecuteAsync(
        GetInternshipRequest request,
        CancellationToken cancellationToken = default)
    {
        var internship = await _internshipRepository.GetByIdAsync(request.InternshipId, cancellationToken);

        if (internship == null)
            return Result<InternshipResponse>.Failure(new Error(
                "Internship.NotFound",
                $"Internship with ID {request.InternshipId} was not found.",
                ErrorType.NotFound));

        var response = new InternshipResponse(
            internship.Id,
            internship.Title,
            internship.Capacity,
            internship.MinimumLevel);

        return Result<InternshipResponse>.Success(response);
    }
}