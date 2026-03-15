using InternshipTracker.Domain.Entities;
using InternshipTracker.Domain.Exceptions;

namespace InternshipTracker.Domain.Factories;

public class InternshipApplicationFactory
{
    public InternshipApplicationFactory()
    {
    }

    public InternshipApplication Create(User candidate, Internship internship)
    {
        if (candidate.Level < internship.MinimumLevel)
        {
            throw new UnderqualifiedException(
                $"Candidate level '{candidate.Level}' does not meet the minimum requirement of '{internship.MinimumLevel}'.");
        }

        var application = new InternshipApplication(Guid.NewGuid(), candidate, internship);

        internship.TrackApplication(application);
        candidate.TrackApplication(application);

        return application;
    }
}