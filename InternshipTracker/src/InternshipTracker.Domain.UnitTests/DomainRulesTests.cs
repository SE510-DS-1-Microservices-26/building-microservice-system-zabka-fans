using InternshipTracker.Domain.Entities;
using InternshipTracker.Domain.Enums;
using InternshipTracker.Domain.Exceptions;
using InternshipTracker.Domain.Factories;
using InternshipTracker.Domain.Interfaces;
using NSubstitute;

namespace InternshipTracker.UnitTests;

public class DomainTests
{
    [Test]
    public void MarkAsRejected_WhenStatusIsEnrolled_ThrowsInvalidApplicationStateException()
    {
        // Arrange
        var candidate = new User(Guid.NewGuid(), "John Doe", CandidateLevel.Junior);
        var internship = new Internship(Guid.NewGuid(), "Software Intern", 11, CandidateLevel.Junior);
        var application = new InternshipApplication(Guid.NewGuid(), candidate, internship);
        
        application.MarkAsAccepted();
        application.MarkAsEnrolled();
        
        // Act & Assert
        Assert.Throws<InvalidApplicationStateException>(() => application.MarkAsRejected());
    }
    
    [Test]
    public void Factory_RejectsUnderqualifiedCandidate()
    {
        // Arrange
        var candidate = new User(Guid.NewGuid(), "Jane Doe", CandidateLevel.Junior);
        var internship = new Internship(Guid.NewGuid(), "Senior Software Intern", 11, CandidateLevel.Senior);
        
        var duplicationChecker = Substitute.For<IDuplicateApplicationChecker>();
        duplicationChecker
            .HasAppliedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var factory = new InternshipApplicationFactory(duplicationChecker);
        
        // Act & Assert
        Assert.ThrowsAsync<UnderqualifiedException>(() => factory.CreateAsync(candidate, internship));
    }

    [Test]
    public void OfferPosition_WhenCapacityIsFull_ThrowsInvalidExceededException()
    {
        // Arrange
        var internship = new Internship(Guid.NewGuid(), "Software Intern", 2, CandidateLevel.Junior);
        var candidate = new User(Guid.NewGuid(), "John Doe", CandidateLevel.Junior);
        var application = new InternshipApplication(Guid.NewGuid(), candidate, internship);
        
        var capacityChecker = Substitute.For<IInternshipCapacityChecker>();
        capacityChecker
            .CountReservedSpotsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(2);
        
        // Act & Assert
        Assert.ThrowsAsync<CapacityExceededException>(() => internship.OfferPositionAsync(application, capacityChecker));
    }
}