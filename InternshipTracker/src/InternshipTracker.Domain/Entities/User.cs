using System.Net.Mime;
using InternshipTracker.Domain.Enums;
using InternshipTracker.Domain.Exceptions;

namespace InternshipTracker.Domain.Entities;

public class User : IEntity
{
    public Guid Id { get; init; }
    public string Name { get; private set; }
    public CandidateLevel Level { get; private set; }

    private readonly List<InternshipApplication> _applications = new();
    public IReadOnlyCollection<InternshipApplication> Applications => _applications.AsReadOnly();

    public User(Guid id, string name, CandidateLevel level)
    {
        Id = id;
        Name = name;
        Level = level;
    }

    // Exclusive Enrollment
    public void Enroll(InternshipApplication application)
    {
        if (!_applications.Contains(application))
            throw new ApplicationMismatchException("This application does not belong to the current user.");

        if (application.Status != ApplicationStatus.Accepted)
            throw new InvalidApplicationStateException(
                $"Cannot enroll. Application status is currently '{application.Status}', expected 'Accepted'.");

        bool isAlreadyEnrolled = _applications.Any(a => a.Status == ApplicationStatus.Enrolled);
        if (isAlreadyEnrolled)
            throw new AlreadyEnrolledException("Candidate is already officially enrolled in another internship.");

        application.MarkAsEnrolled();
    }

    internal void TrackApplication(InternshipApplication application)
    {
        if (_applications.Contains(application))
        {
            throw new DuplicateApplicationException("This application is already being tracked by the candidate.");
        }

        _applications.Add(application);
    }
}