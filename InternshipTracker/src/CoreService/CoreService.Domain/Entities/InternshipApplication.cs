using CoreService.Domain.Enums;
using CoreService.Domain.Exceptions;

namespace CoreService.Domain.Entities;

public class InternshipApplication
{
    public Guid Id { get; init; }
    public Guid CandidateId { get; private set; }
    public CandidateLevel CandidateLevel { get; private set; }
    public UserCore Candidate { get; private set; }
    public Internship Internship { get; private set; }
    public ApplicationStatus Status { get; private set; }
    
    public InternshipApplication(Guid id, Guid candidateId, CandidateLevel level, Internship internship, UserCore candidate)
    {
        Id = id;
        CandidateId = candidateId;
        CandidateLevel = level;
        Internship = internship;
        Candidate = candidate;
        Status = ApplicationStatus.Pending;
    }
    
    private InternshipApplication() { }

    public void MarkAsAccepted()
    {
        Status = ApplicationStatus.Accepted;
    }

    /// <summary>Transitions the application to Enrolling, locking it while the onboarding saga runs.</summary>
    public void MarkAsEnrolling()
    {
        if (Status != ApplicationStatus.Accepted)
            throw new InvalidApplicationStateException(
                $"Cannot begin enrollment from status {Status}. Must be Accepted.");

        Status = ApplicationStatus.Enrolling;
    }

    /// <summary>Marks the application as fully enrolled once the saga completes successfully.</summary>
    public void MarkAsEnrolled()
    {
        if (Status != ApplicationStatus.Enrolling)
            throw new InvalidApplicationStateException(
                $"Cannot finalize enrollment from status {Status}. Must be Enrolling.");

        Status = ApplicationStatus.Enrolled;
    }

    /// <summary>Reverts the application back to Accepted when the saga needs to compensate a failed step.</summary>
    public void RevertToAccepted()
    {
        if (Status != ApplicationStatus.Enrolling)
            throw new InvalidApplicationStateException(
                $"Cannot revert to Accepted from status {Status}. Must be Enrolling.");

        Status = ApplicationStatus.Accepted;
    }

    /// <summary>Records a partial fault: the IT account was created but the welcome email could not be sent.</summary>
    public void MarkAsEnrolledNotificationFault()
    {
        if (Status != ApplicationStatus.Enrolling)
            throw new InvalidApplicationStateException(
                $"Cannot mark as notification fault from status {Status}. Must be Enrolling.");

        Status = ApplicationStatus.EnrolledNotificationFault;
    }

    public void MarkAsRejected()
    {
        if (Status is ApplicationStatus.Enrolled or ApplicationStatus.Enrolling or ApplicationStatus.EnrolledNotificationFault)
            throw new InvalidApplicationStateException(
                $"Cannot reject an application in status {Status}.");

        Status = ApplicationStatus.Rejected;
    }
}