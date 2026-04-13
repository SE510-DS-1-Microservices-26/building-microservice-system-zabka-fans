using CoreService.Domain.Entities;
using CoreService.Domain.Enums;
using CoreService.Domain.Exceptions;

namespace InternshipTracker.Tests;

public class ApplicationStateTransitionTests
{
    private const string DefaultCandidateName = "Test Candidate";
    private const string DefaultCandidateEmail = "candidate@example.com";
    private const string DefaultInternshipTitle = "Test Internship";
    private const int DefaultCapacity = 10;
    private static readonly CandidateLevel DefaultLevel = CandidateLevel.Junior;

    private static InternshipApplication CreateApplication(ApplicationStatus targetStatus = ApplicationStatus.Pending)
    {
        var candidate = new UserCore(Guid.NewGuid(), DefaultCandidateName, DefaultCandidateEmail, DefaultLevel);
        var internship = new Internship(Guid.NewGuid(), DefaultInternshipTitle, DefaultCapacity, DefaultLevel);
        var app = new InternshipApplication(Guid.NewGuid(), internship, candidate);

        if (targetStatus >= ApplicationStatus.Accepted) app.MarkAsAccepted();
        if (targetStatus >= ApplicationStatus.Enrolling) app.MarkAsEnrolling();
        if (targetStatus >= ApplicationStatus.Enrolled) app.MarkAsEnrolled();

        return app;
    }

    [Test]
    public void NewApplication_HasPendingStatus()
    {
        var app = CreateApplication();

        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Pending));
    }

    [Test]
    public void MarkAsAccepted_FromPending_Succeeds()
    {
        var app = CreateApplication(ApplicationStatus.Pending);

        app.MarkAsAccepted();

        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Accepted));
    }

    [Test]
    public void MarkAsEnrolling_FromAccepted_Succeeds()
    {
        var app = CreateApplication(ApplicationStatus.Accepted);

        app.MarkAsEnrolling();

        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Enrolling));
    }

    [TestCase(ApplicationStatus.Pending)]
    [TestCase(ApplicationStatus.Enrolling)]
    [TestCase(ApplicationStatus.Enrolled)]
    public void MarkAsEnrolling_FromInvalidStatus_ThrowsInvalidApplicationState(ApplicationStatus startStatus)
    {
        var app = CreateApplication(startStatus);

        Assert.Throws<InvalidApplicationStateException>(() => app.MarkAsEnrolling());
    }

    [Test]
    public void MarkAsEnrolled_FromEnrolling_Succeeds()
    {
        var app = CreateApplication(ApplicationStatus.Enrolling);

        app.MarkAsEnrolled();

        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Enrolled));
    }

    [TestCase(ApplicationStatus.Pending)]
    [TestCase(ApplicationStatus.Accepted)]
    [TestCase(ApplicationStatus.Enrolled)]
    public void MarkAsEnrolled_FromInvalidStatus_ThrowsInvalidApplicationState(ApplicationStatus startStatus)
    {
        var app = CreateApplication(startStatus);

        Assert.Throws<InvalidApplicationStateException>(() => app.MarkAsEnrolled());
    }

    [Test]
    public void RevertToAccepted_FromEnrolling_Succeeds()
    {
        var app = CreateApplication(ApplicationStatus.Enrolling);

        app.RevertToAccepted();

        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Accepted));
    }

    [TestCase(ApplicationStatus.Pending)]
    [TestCase(ApplicationStatus.Accepted)]
    [TestCase(ApplicationStatus.Enrolled)]
    public void RevertToAccepted_FromInvalidStatus_ThrowsInvalidApplicationState(ApplicationStatus startStatus)
    {
        var app = CreateApplication(startStatus);

        Assert.Throws<InvalidApplicationStateException>(() => app.RevertToAccepted());
    }

    [Test]
    public void MarkAsEnrolledNotificationFault_FromEnrolling_Succeeds()
    {
        var app = CreateApplication(ApplicationStatus.Enrolling);

        app.MarkAsEnrolledNotificationFault();

        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.EnrolledNotificationFault));
    }

    [TestCase(ApplicationStatus.Pending)]
    [TestCase(ApplicationStatus.Accepted)]
    [TestCase(ApplicationStatus.Enrolled)]
    public void MarkAsEnrolledNotificationFault_FromInvalidStatus_ThrowsInvalidApplicationState(
        ApplicationStatus startStatus)
    {
        var app = CreateApplication(startStatus);

        Assert.Throws<InvalidApplicationStateException>(() => app.MarkAsEnrolledNotificationFault());
    }

    [TestCase(ApplicationStatus.Pending)]
    [TestCase(ApplicationStatus.Accepted)]
    public void MarkAsRejected_FromRejectableStatus_Succeeds(ApplicationStatus startStatus)
    {
        var app = CreateApplication(startStatus);

        app.MarkAsRejected();

        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Rejected));
    }

    [TestCase(ApplicationStatus.Enrolling)]
    [TestCase(ApplicationStatus.Enrolled)]
    public void MarkAsRejected_FromNonRejectableStatus_ThrowsInvalidApplicationState(ApplicationStatus startStatus)
    {
        var app = CreateApplication(startStatus);

        Assert.Throws<InvalidApplicationStateException>(() => app.MarkAsRejected());
    }

    [Test]
    public void MarkAsRejected_FromEnrolledNotificationFault_ThrowsInvalidApplicationState()
    {
        // EnrolledNotificationFault is off the linear path, so build it manually
        var candidate = new UserCore(Guid.NewGuid(), DefaultCandidateName, DefaultCandidateEmail, DefaultLevel);
        var internship = new Internship(Guid.NewGuid(), DefaultInternshipTitle, DefaultCapacity, DefaultLevel);
        var app = new InternshipApplication(Guid.NewGuid(), internship, candidate);
        app.MarkAsAccepted();
        app.MarkAsEnrolling();
        app.MarkAsEnrolledNotificationFault();

        Assert.Throws<InvalidApplicationStateException>(() => app.MarkAsRejected());
    }

    [Test]
    public void FullHappyPath_PendingToEnrolled_AllTransitionsSucceed()
    {
        var app = CreateApplication(ApplicationStatus.Pending);

        app.MarkAsAccepted();
        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Accepted));

        app.MarkAsEnrolling();
        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Enrolling));

        app.MarkAsEnrolled();
        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Enrolled));
    }

    [Test]
    public void CompensationPath_EnrollingRevertedToAccepted_ThenRejectableAgain()
    {
        var app = CreateApplication(ApplicationStatus.Enrolling);

        app.RevertToAccepted();
        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Accepted));

        // after compensation the application is unlocked and can be rejected
        app.MarkAsRejected();
        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Rejected));
    }
}
