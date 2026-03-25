using CoreService.Domain.Enums;
using CoreService.Domain.Exceptions;

namespace CoreService.Domain.Entities;

public class InternshipApplication
{
    public Guid Id { get; init; }
    public Guid CandidateId { get; private set; }
    public CandidateLevel CandidateLevel { get; private set; }
    public Internship Internship { get; private set; }
    public ApplicationStatus Status { get; private set; }
    
    public InternshipApplication(Guid id, Guid candidateId, CandidateLevel level, Internship internship)
    {
        Id = id;
        CandidateId = candidateId;
        CandidateLevel = level;
        Internship = internship;
        Status = ApplicationStatus.Pending;
    }
    
    private InternshipApplication() { }

    public void MarkAsAccepted()
    {
        Status = ApplicationStatus.Accepted;
    }

    public void MarkAsEnrolled()
    {
        Status = ApplicationStatus.Enrolled;
    }

    public void MarkAsRejected()
    {
        if (Status == ApplicationStatus.Enrolled)
            throw new InvalidApplicationStateException(
                "Cannot reject an application after the candidate has already officially enrolled.");

        Status = ApplicationStatus.Rejected;
    }
}