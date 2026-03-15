using System.Net.Mime;
using InternshipTracker.Domain.Enums;
using InternshipTracker.Domain.Exceptions;

namespace InternshipTracker.Domain.Entities;

public class User : IEntity
{
    public Guid Id { get; set; }
    public string Name { get; private set; }
    public CandidateLevel Level { get; private set; }

    // Encapsulated collection to prevent external manipulation
    private readonly List<InternshipApplication> _applications = new();
    public IReadOnlyCollection<InternshipApplication> Applications => _applications.AsReadOnly();

    public User(Guid id, string name, CandidateLevel level)
    {
        Id = id;
        Name = name;
        Level = level;
    }

    // Exclusive Enrollment
    public void Enroll(InternshipApplication internshipApplication)
    {
        if (!_applications.Contains(internshipApplication))
            throw new InvalidOperationException("Application does not belong to this user.");

        if (internshipApplication.Status != ApplicationStatus.Accepted)
            throw new DomainException("Can only enroll in applications that have been accepted by the company.");

        bool isAlreadyEnrolled = _applications.Any(a => a.Status == ApplicationStatus.Enrolled);
        if (isAlreadyEnrolled)
        {
            throw new DomainException("Candidate is already officially enrolled in another internship.");
        }

        internshipApplication.MarkAsEnrolled();
    }

    // Internal access allows the Internship entity to link applications safely 
    // without exposing the list to the Application/Infrastructure layers.
    internal void TrackApplication(InternshipApplication application)
    {
        _applications.Add(application);
    }
}