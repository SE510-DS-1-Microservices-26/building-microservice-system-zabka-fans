using CoreService.Application.DTOs.Requests;
using CoreService.Application.Enums;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Application.UseCases;
using CoreService.Domain.Entities;
using CoreService.Domain.Enums;
using CoreService.Domain.Exceptions;
using CoreService.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InternshipTracker.Tests;

public class ChangeApplicationStatusUseCaseTests
{
    private IInternshipApplicationRepository _appRepo = null!;
    private IInternshipCapacityChecker _capacityChecker = null!;
    private IPublishEndpoint _publishEndpoint = null!;
    private ChangeApplicationStatusUseCase _useCase = null!;

    private InternshipApplication CreateApplication(
        ApplicationStatus initialStatus = ApplicationStatus.Pending,
        int capacity = 10)
    {
        var candidate = new UserCore(Guid.NewGuid(), "Test User", "test.user@example.com", CandidateLevel.Junior);
        var internship = new Internship(Guid.NewGuid(), "Test Internship", capacity, CandidateLevel.Junior);
        var app = new InternshipApplication(Guid.NewGuid(), candidate.Id, candidate.Level, internship, candidate);

        if (initialStatus >= ApplicationStatus.Accepted) app.MarkAsAccepted();
        if (initialStatus >= ApplicationStatus.Enrolling) app.MarkAsEnrolling();
        if (initialStatus >= ApplicationStatus.Enrolled) app.MarkAsEnrolled();

        return app;
    }

    [SetUp]
    public void SetUp()
    {
        _appRepo = Substitute.For<IInternshipApplicationRepository>();
        _appRepo.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());
        _capacityChecker = Substitute.For<IInternshipCapacityChecker>();
        _capacityChecker.CountReservedSpotsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(0);
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _useCase = new ChangeApplicationStatusUseCase(
            _appRepo, _capacityChecker, _publishEndpoint,
            Substitute.For<ILogger<ChangeApplicationStatusUseCase>>());
    }

    [Test]
    public async Task ChangeStatus_ApplicationNotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _appRepo.GetWithDetailsAsync(id, Arg.Any<CancellationToken>()).Returns((InternshipApplication?)null);

        var result = await _useCase.ExecuteAsync(new ChangeApplicationStatusRequest(id, ApplicationStatus.Accepted));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo("Application.NotFound"));
        Assert.That(result.Error.Type, Is.EqualTo(ErrorType.NotFound));
    }

    [Test]
    public async Task ChangeStatus_AcceptPending_Succeeds()
    {
        var app = CreateApplication(ApplicationStatus.Pending);
        _appRepo.GetWithDetailsAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);

        var result = await _useCase.ExecuteAsync(new ChangeApplicationStatusRequest(app.Id, ApplicationStatus.Accepted));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Accepted));
    }

    [Test]
    public async Task ChangeStatus_EnrollAccepted_SetsEnrollingState()
    {
        var app = CreateApplication(ApplicationStatus.Accepted);
        _appRepo.GetWithDetailsAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);
        _appRepo.HasStatusAsync(app.CandidateId, ApplicationStatus.Enrolled, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _useCase.ExecuteAsync(new ChangeApplicationStatusRequest(app.Id, ApplicationStatus.Enrolled));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Enrolling));
    }

    [Test]
    public async Task ChangeStatus_RejectPending_Succeeds()
    {
        var app = CreateApplication(ApplicationStatus.Pending);
        _appRepo.GetWithDetailsAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);

        var result = await _useCase.ExecuteAsync(new ChangeApplicationStatusRequest(app.Id, ApplicationStatus.Rejected));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(app.Status, Is.EqualTo(ApplicationStatus.Rejected));
    }

    [Test]
    public async Task ChangeStatus_RevertToPending_ReturnsFailure()
    {
        var app = CreateApplication(ApplicationStatus.Accepted);
        _appRepo.GetWithDetailsAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);

        var result = await _useCase.ExecuteAsync(new ChangeApplicationStatusRequest(app.Id, ApplicationStatus.Pending));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo("Application.InvalidTransition"));
    }

    [Test]
    public void ChangeStatus_EnrollWhenPending_ThrowsInvalidApplicationState()
    {
        var app = CreateApplication(ApplicationStatus.Pending);
        _appRepo.GetWithDetailsAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);

        Assert.ThrowsAsync<InvalidApplicationStateException>(() =>
            _useCase.ExecuteAsync(new ChangeApplicationStatusRequest(app.Id, ApplicationStatus.Enrolled)));
    }

    [Test]
    public void ChangeStatus_EnrollWhenAlreadyEnrolledElsewhere_ThrowsAlreadyEnrolled()
    {
        var app = CreateApplication(ApplicationStatus.Accepted);
        _appRepo.GetWithDetailsAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);
        _appRepo.HasStatusAsync(app.CandidateId, ApplicationStatus.Enrolled, Arg.Any<CancellationToken>()).Returns(true);

        Assert.ThrowsAsync<AlreadyEnrolledException>(() =>
            _useCase.ExecuteAsync(new ChangeApplicationStatusRequest(app.Id, ApplicationStatus.Enrolled)));
    }

    [Test]
    public void ChangeStatus_AcceptWhenCapacityFull_ThrowsCapacityExceeded()
    {
        var app = CreateApplication(ApplicationStatus.Pending, capacity: 2);
        _appRepo.GetWithDetailsAsync(app.Id, Arg.Any<CancellationToken>()).Returns(app);
        _capacityChecker.CountReservedSpotsAsync(app.Internship.Id, Arg.Any<CancellationToken>()).Returns(2);

        Assert.ThrowsAsync<CapacityExceededException>(() =>
            _useCase.ExecuteAsync(new ChangeApplicationStatusRequest(app.Id, ApplicationStatus.Accepted)));
    }
}
