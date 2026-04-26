using CoreService.Application.DTOs;
using CoreService.Application.DTOs.Requests;
using CoreService.Application.DTOs.Responses;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CoreService.Application.UseCases;

public class CreateInternshipUseCase : IUseCase<CreateInternshipRequest, InternshipResponse>
{
    private readonly IInternshipRepository _internshipRepository;
    private readonly ILogger<CreateInternshipUseCase> _logger;

    public CreateInternshipUseCase(IInternshipRepository internshipRepository,
        ILogger<CreateInternshipUseCase> logger)
    {
        _internshipRepository = internshipRepository;
        _logger = logger;
    }

    public async Task<Result<InternshipResponse>> ExecuteAsync(
        CreateInternshipRequest request,
        CancellationToken cancellationToken = default)
    {
        var internship = new Internship(Guid.NewGuid(), request.Title, request.Capacity, request.MinimumLevel);

        await _internshipRepository.AddAsync(internship, cancellationToken);
        await _internshipRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Internship {InternshipId} created: {Title}, capacity {Capacity}",
            internship.Id, internship.Title, internship.Capacity);

        var response = new InternshipResponse(internship.Id, internship.Title, internship.Capacity,
            internship.MinimumLevel);
        return Result<InternshipResponse>.Success(response);
    }
}