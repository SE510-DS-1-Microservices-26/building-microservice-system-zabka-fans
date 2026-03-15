using System.Net.Mime;
using InternshipTracker.Domain.Enums;
using InternshipTracker.Domain.Exceptions;
using InternshipTracker.Domain.Interfaces;

namespace InternshipTracker.Domain.Entities;

public class User : IEntity
{
    public Guid Id { get; init; }
    public string Name { get; private set; }
    public CandidateLevel Level { get; private set; }
    public User(Guid id, string name, CandidateLevel level)
    {
        Id = id;
        Name = name;
        Level = level;
    }

    // Exclusive Enrollment
    public async Task EnrollAsync(
        InternshipApplication application, 
        IUserEnrollmentChecker enrollmentChecker, 
        CancellationToken cancellationToken = default)
    {
        // 1. Safety check using IDs
        if (application.Candidate.Id != this.Id)
            throw new ApplicationMismatchException("This application does not belong to the current user.");

        if (application.Status != ApplicationStatus.Accepted)
            throw new InvalidApplicationStateException($"Cannot enroll. Application status is currently '{application.Status}', expected 'Accepted'.");

        // 2. Await the DB-optimized check via the injected domain service
        bool isAlreadyEnrolled = await enrollmentChecker.IsAlreadyEnrolledAsync(this.Id, cancellationToken);
        
        if (isAlreadyEnrolled)
            throw new AlreadyEnrolledException("Candidate is already officially enrolled in another internship.");

        // 3. Mutate the state
        application.MarkAsEnrolled();
    }
}