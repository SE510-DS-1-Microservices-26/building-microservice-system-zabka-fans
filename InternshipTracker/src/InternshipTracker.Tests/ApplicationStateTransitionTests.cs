using CoreService.Domain.Entities;
using CoreService.Domain.Enums;
using CoreService.Domain.Exceptions;

namespace InternshipTracker.Tests;

/// <summary>
/// Tests every legal and illegal state transition on <see cref="InternshipApplication"/>,
/// covering the full saga lifecycle: Pending → Accepted → Enrolling → Enrolled / compensation paths.
/// </summary>
public class ApplicationStateTransitionTests
{
    // ── Shared fixture builder ──────────────────────────────────────────────

    private const string DefaultCandidateName = "Test Candidate";
    private const string DefaultCandidateEmail = "candidate@example.com";
    private const string DefaultInternshipTitle = "Test Internship";
    private const int DefaultCapacity = 10;
    private static readonly CandidateLevel DefaultLevel = CandidateLevel.Junior;

    private static InternshipApplication CreateApplication(ApplicationStatus targetStatus = ApplicationStatus.Pending)
    {
        var candidate = new UserCore(Guid.NewGuid(), DefaultCandidateName, DefaultCandidateEmail, DefaultLevel);
        var internship = new Internship(Guid.NewGuid(), DefaultInternshipTitle, DefaultCapacity, DefaultLevel);
        var app = new InternshipApplication(Guid.NewGuid(), candidate.Id, candidate.Level, internship, candidate);

        if (targetStatus >= ApplicationStatus.Accepted) app.MarkAsAccepted();
        if (targetStatus >= ApplicationStatus.Enrolling) app.MarkAsEnrolling();
        if (targetStatus >= ApplicationStatus.Enrolled) app.MarkAsEnrolled();

        return app;
    }

    // ── Constructor ─────────────────────────────────────────────────────────

    [Test]
    public void NewApplication_HasPendingStatus()
    {
        var app = CreateApplication();

        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Pending));
    }

    // ── MarkAsAccepted ──────────────────────────────────────────────────────

    [Test]
    public void MarkAsAccepted_FromPending_Succeeds()
    {
        var app = CreateApplication(ApplicationStatus.Pending);

        app.MarkAsAccepted();

        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Accepted));
    }

    // ── MarkAsEnrolling ─────────────────────────────────────────────────────

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

    // ── MarkAsEnrolled (saga finalize) ──────────────────────────────────────

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

    // ── RevertToAccepted (saga compensation) ────────────────────────────────

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

    // ── MarkAsEnrolledNotificationFault (saga notification failure) ─────────

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

    // ── MarkAsRejected ──────────────────────────────────────────────────────

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
        // EnrolledNotificationFault can't be built via the helper (it's not on the linear path),
        // so we build it manually: Accepted → Enrolling → MarkAsEnrolledNotificationFault
        var candidate = new UserCore(Guid.NewGuid(), DefaultCandidateName, DefaultCandidateEmail, DefaultLevel);
        var internship = new Internship(Guid.NewGuid(), DefaultInternshipTitle, DefaultCapacity, DefaultLevel);
        var app = new InternshipApplication(Guid.NewGuid(), candidate.Id, candidate.Level, internship, candidate);
        app.MarkAsAccepted();
        app.MarkAsEnrolling();
        app.MarkAsEnrolledNotificationFault();

        Assert.Throws<InvalidApplicationStateException>(() => app.MarkAsRejected());
    }

    // ── Full happy path ─────────────────────────────────────────────────────

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

    // ── Full compensation path ──────────────────────────────────────────────

    [Test]
    public void CompensationPath_EnrollingRevertedToAccepted_ThenRejectableAgain()
    {
        var app = CreateApplication(ApplicationStatus.Enrolling);

        app.RevertToAccepted();
        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Accepted));

        // After compensation, the application is unlocked and can be rejected
        app.MarkAsRejected();
        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Rejected));
    }
}

