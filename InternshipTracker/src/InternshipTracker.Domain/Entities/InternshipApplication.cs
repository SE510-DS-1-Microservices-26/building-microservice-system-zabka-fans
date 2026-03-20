using InternshipTracker.Domain.Enums;
using InternshipTracker.Domain.Exceptions;

namespace InternshipTracker.Domain.Entities;

public class InternshipApplication : IEntity
{
    public Guid Id { get; init; }
    public User Candidate { get; private set; }
    public Internship Internship { get; private set; }
    public ApplicationStatus Status { get; private set; }

    // for EF core 
    private InternshipApplication()
    {
    }

    internal InternshipApplication(Guid id, User candidate, Internship internship)
    {
        Id = id;
        Candidate = candidate;
        Internship = internship;
        Status = ApplicationStatus.Pending;
    }

    internal void MarkAsAccepted() => Status = ApplicationStatus.Accepted;
    internal void MarkAsEnrolled() => Status = ApplicationStatus.Enrolled;

    public void MarkAsRejected()
    {
        if (Status == ApplicationStatus.Enrolled)
        {
            throw new InvalidApplicationStateException(
                "Cannot reject an application after the candidate has already officially enrolled.");
        }

        Status = ApplicationStatus.Rejected;
    }
}