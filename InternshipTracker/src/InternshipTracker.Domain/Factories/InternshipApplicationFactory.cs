using InternshipTracker.Domain.Entities;
using InternshipTracker.Domain.Exceptions;
using InternshipTracker.Domain.Interfaces;

namespace InternshipTracker.Domain.Factories;

public class InternshipApplicationFactory
{
    private readonly IDuplicateApplicationChecker _duplicationChecker;

    public InternshipApplicationFactory(IDuplicateApplicationChecker duplicationChecker)
    {
        _duplicationChecker = duplicationChecker;
    }

    public async Task<InternshipApplication> CreateAsync(
        User candidate,
        Internship internship,
        CancellationToken cancellationToken = default)
    {
        if (candidate.Level < internship.MinimumLevel)
        {
            throw new UnderqualifiedException($"Candidate level '{candidate.Level}' does not meet the requirement.");
        }

        bool hasApplied = await _duplicationChecker.HasAppliedAsync(candidate.Id, internship.Id, cancellationToken);
        if (hasApplied)
        {
            throw new DuplicateApplicationException("The candidate has already applied to this internship.");
        }

        return new InternshipApplication(Guid.NewGuid(), candidate, internship);
    }
}