using InternshipTracker.Domain.Enums;

namespace InternshipTracker.Domain.Entities;

public class InternshipApplication : IEntity
{
    public Guid Id { get; set; }
    public User Candidate { get; private set; }
    public Internship Internship { get; private set; }
    public ApplicationStatus Status { get; private set; }

    // Internal constructor ensures InternshipApplication can only be created via Internship.ReceiveApplication()
    internal InternshipApplication(Guid id, User candidate, Internship internship)
    {
        Id = id;
        Candidate = candidate;
        Internship = internship;
        Status = ApplicationStatus.Pending;
    }

    internal void MarkAsAccepted() => Status = ApplicationStatus.Accepted;
    internal void MarkAsEnrolled() => Status = ApplicationStatus.Enrolled;
    internal void MarkAsRejected() => Status = ApplicationStatus.Rejected;
}