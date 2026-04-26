using CoreService.Application.DTOs.Requests;
using CoreService.Application.Enums;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Application.UseCases;
using CoreService.Domain.Entities;
using CoreService.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InternshipTracker.Tests;

public class GetApplicationByIdUseCaseTests
{
    private IInternshipApplicationRepository _appRepo = null!;
    private GetApplicationByIdUseCase _useCase = null!;

    [SetUp]
    public void SetUp()
    {
        _appRepo = Substitute.For<IInternshipApplicationRepository>();
        _useCase = new GetApplicationByIdUseCase(
            _appRepo,
            Substitute.For<ILogger<GetApplicationByIdUseCase>>());
    }

    [Test]
    public async Task GetById_ExistingApplication_ReturnsSuccess()
    {
        // Arrange
        var candidate = new UserCore(Guid.NewGuid(), "Alice Smith", "alice@example.com", CandidateLevel.Junior);
        var internship = new Internship(Guid.NewGuid(), "Summer Internship", 10, CandidateLevel.Junior);
        var application = new InternshipApplication(Guid.NewGuid(), internship, candidate);

        _appRepo.GetWithDetailsAsync(application.Id, Arg.Any<CancellationToken>())
            .Returns(application);

        // Act
        var result = await _useCase.ExecuteAsync(new GetApplicationRequest(application.Id));

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value!.Id, Is.EqualTo(application.Id));
            Assert.That(result.Value.CandidateId, Is.EqualTo(candidate.Id));
            Assert.That(result.Value.CandidateName, Is.EqualTo("Alice Smith"));
            Assert.That(result.Value.CandidateLevel, Is.EqualTo(CandidateLevel.Junior));
            Assert.That(result.Value.InternshipId, Is.EqualTo(internship.Id));
            Assert.That(result.Value.InternshipTitle, Is.EqualTo("Summer Internship"));
            Assert.That(result.Value.Status, Is.EqualTo(ApplicationStatus.Pending));
        });
    }

    [Test]
    public async Task GetById_NonExistentApplication_ReturnsNotFoundFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _appRepo.GetWithDetailsAsync(id, Arg.Any<CancellationToken>())
            .Returns((InternshipApplication?)null);

        // Act
        var result = await _useCase.ExecuteAsync(new GetApplicationRequest(id));

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(result.Error!.Code, Is.EqualTo("Application.NotFound"));
            Assert.That(result.Error.Type, Is.EqualTo(ErrorType.NotFound));
        });
    }

    [Test]
    public async Task GetById_AcceptedApplication_ReturnsCorrectStatus()
    {
        // Arrange
        var candidate = new UserCore(Guid.NewGuid(), "Bob Jones", "bob@example.com", CandidateLevel.Middle);
        var internship = new Internship(Guid.NewGuid(), "Backend Internship", 5, CandidateLevel.Junior);
        var application = new InternshipApplication(Guid.NewGuid(), internship, candidate);
        application.MarkAsAccepted();

        _appRepo.GetWithDetailsAsync(application.Id, Arg.Any<CancellationToken>())
            .Returns(application);

        // Act
        var result = await _useCase.ExecuteAsync(new GetApplicationRequest(application.Id));

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Status, Is.EqualTo(ApplicationStatus.Accepted));
    }
}

