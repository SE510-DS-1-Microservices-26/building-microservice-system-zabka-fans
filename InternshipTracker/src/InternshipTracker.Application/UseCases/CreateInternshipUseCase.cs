using InternshipTracker.Application.DTOs;
using InternshipTracker.Application.DTOs.Requests;
using InternshipTracker.Application.DTOs.Responses;
using InternshipTracker.Application.Enums;
using InternshipTracker.Application.Interfaces;
using InternshipTracker.Application.Interfaces.Repositories;
using InternshipTracker.Domain.Entities;

namespace InternshipTracker.Application.UseCases;

public class CreateInternshipUseCase : IUseCase<CreateInternshipRequest, InternshipResponse>
{
    private readonly IInternshipRepository _internshipRepository;

    public CreateInternshipUseCase(IInternshipRepository internshipRepository)
    {
        _internshipRepository = internshipRepository;
    }

    public async Task<Result<InternshipResponse>> ExecuteAsync(
        CreateInternshipRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var internship = new Internship(Guid.NewGuid(), request.Title, request.Capacity, request.MinimumLevel);

            await _internshipRepository.AddAsync(internship, cancellationToken);
            await _internshipRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            var response = new InternshipResponse(internship.Id, internship.Title, internship.Capacity,
                internship.MinimumLevel);
            return Result<InternshipResponse>.Success(response);
        }
        catch (Exception)
        {
            // Maps to a 500 Internal Server Error in the UI
            return Result<InternshipResponse>.Failure(new Error(
                "System.Failure",
                "An unexpected error occurred while creating the internship.",
                ErrorType.Failure));
        }
    }
}