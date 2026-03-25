using CoreService.Domain.Entities;
using CoreService.Domain.Enums;
using CoreService.Domain.Exceptions;
using CoreService.Domain.Interfaces;

namespace CoreService.Application.Factories;

public class InternshipApplicationFactory
{
    private readonly IDuplicateApplicationChecker _duplicationChecker;

    public InternshipApplicationFactory(IDuplicateApplicationChecker duplicationChecker)
    {
        _duplicationChecker = duplicationChecker;
    }

    public async Task<InternshipApplication> CreateAsync(
        Guid candidateId,
        CandidateLevel candidateLevel,
        Internship internship,
        CancellationToken cancellationToken = default)
    {
        if (candidateLevel < internship.MinimumLevel)
            throw new UnderqualifiedException($"Candidate level '{candidateLevel}' does not meet the requirement.");

        var hasApplied = await _duplicationChecker.HasAppliedAsync(candidateId, internship.Id, cancellationToken);
        if (hasApplied)
            throw new DuplicateApplicationException("The candidate has already applied to this internship.");

        return new InternshipApplication(Guid.NewGuid(), candidateId, candidateLevel, internship);
    }
}