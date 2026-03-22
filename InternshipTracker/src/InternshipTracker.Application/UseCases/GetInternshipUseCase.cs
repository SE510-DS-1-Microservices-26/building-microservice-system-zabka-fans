using InternshipTracker.Application.DTOs;
using InternshipTracker.Application.DTOs.Requests;
using InternshipTracker.Application.DTOs.Responses;
using InternshipTracker.Application.Enums;
using InternshipTracker.Application.Interfaces;
using InternshipTracker.Application.Interfaces.Repositories;
using InternshipTracker.Domain.Entities;

namespace InternshipTracker.Application.UseCases;

public class GetInternshipUseCase : IUseCase<GetInternshipRequest, InternshipResponse>
{
    private readonly IReadOnlyRepository<Internship> _internshipRepository;

    public GetInternshipUseCase(IReadOnlyRepository<Internship> internshipRepository)
    {
        _internshipRepository = internshipRepository;
    }

    public async Task<Result<InternshipResponse>> ExecuteAsync(
        GetInternshipRequest request,
        CancellationToken cancellationToken = default)
    {
        try
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
        catch (Exception)
        {
            return Result<InternshipResponse>.Failure(new Error(
                "System.Failure",
                "An unexpected error occurred while fetching the internship.",
                ErrorType.Failure));
        }
    }
}