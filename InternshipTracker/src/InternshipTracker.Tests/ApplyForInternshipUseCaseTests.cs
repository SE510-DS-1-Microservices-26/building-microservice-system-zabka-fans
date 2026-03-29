using CoreService.Application.DTOs.Requests;
using CoreService.Application.Enums;
using CoreService.Application.Factories;
using CoreService.Application.Interfaces;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Application.UseCases;
using CoreService.Domain.Entities;
using CoreService.Domain.Enums;
using CoreService.Domain.Exceptions;
using CoreService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InternshipTracker.Tests;

public class ApplyForInternshipUseCaseTests
{
    private IUserCoreRepository _userCoreRepo = null!;
    private IInternshipRepository _internshipRepo = null!;
    private IInternshipApplicationRepository _applicationRepo = null!;
    private IDuplicateApplicationChecker _duplicationChecker = null!;
    private InternshipApplicationFactory _factory = null!;
    private ApplyForInternshipUseCase _useCase = null!;

    [SetUp]
    public void SetUp()
    {
        _userCoreRepo = Substitute.For<IUserCoreRepository>();
        _internshipRepo = Substitute.For<IInternshipRepository>();
        _applicationRepo = Substitute.For<IInternshipApplicationRepository>();
        _applicationRepo.UnitOfWork.Returns(Substitute.For<IUnitOfWork>());

        _duplicationChecker = Substitute.For<IDuplicateApplicationChecker>();
        _duplicationChecker
            .HasAppliedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _factory = new InternshipApplicationFactory(_duplicationChecker);
        _useCase = new ApplyForInternshipUseCase(
            _internshipRepo, _applicationRepo, _factory, _userCoreRepo,
            Substitute.For<ILogger<ApplyForInternshipUseCase>>());
    }

    [Test]
    public async Task ApplyForInternship_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var internshipId = Guid.NewGuid();

        var user = new UserCore(userId, "Test User", CandidateLevel.Junior);
        var internship = new Internship(internshipId, "Test Internship", 10, CandidateLevel.Junior);

        _userCoreRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _internshipRepo.GetByIdAsync(internshipId, Arg.Any<CancellationToken>()).Returns(internship);

        // Act
        var result = await _useCase.ExecuteAsync(new ApplyForInternshipRequest(userId, internshipId));

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Status, Is.EqualTo(ApplicationStatus.Pending));
        await _applicationRepo
            .Received(1)
            .AddAsync(Arg.Any<InternshipApplication>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyForInternship_UserNotSynced_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userCoreRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((UserCore?)null);

        // Act
        var result = await _useCase.ExecuteAsync(new ApplyForInternshipRequest(userId, Guid.NewGuid()));

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo("User.NotSynced"));
        Assert.That(result.Error.Type, Is.EqualTo(ErrorType.Validation));
    }

    [Test]
    public async Task ApplyForInternship_InternshipNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserCore(userId, "Test User", CandidateLevel.Junior);
        _userCoreRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _internshipRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Internship?)null);

        // Act
        var result = await _useCase.ExecuteAsync(new ApplyForInternshipRequest(userId, Guid.NewGuid()));

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo("Internship.NotFound"));
        Assert.That(result.Error.Type, Is.EqualTo(ErrorType.NotFound));
    }

    [Test]
    public void ApplyForInternship_Underqualified_ThrowsDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserCore(userId, "Test User", CandidateLevel.Junior);
        var internship = new Internship(Guid.NewGuid(), "Senior Role", 5, CandidateLevel.Senior);

        _userCoreRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _internshipRepo.GetByIdAsync(internship.Id, Arg.Any<CancellationToken>()).Returns(internship);

        // Act & Assert
        Assert.ThrowsAsync<UnderqualifiedException>(() =>
            _useCase.ExecuteAsync(new ApplyForInternshipRequest(userId, internship.Id)));
    }

    [Test]
    public void ApplyForInternship_DuplicateApplication_ThrowsDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserCore(userId, "Test User", CandidateLevel.Junior);
        var internship = new Internship(Guid.NewGuid(), "Test Internship", 10, CandidateLevel.Junior);

        _userCoreRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _internshipRepo.GetByIdAsync(internship.Id, Arg.Any<CancellationToken>()).Returns(internship);
        _duplicationChecker
            .HasAppliedAsync(userId, internship.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act & Assert
        Assert.ThrowsAsync<DuplicateApplicationException>(() =>
            _useCase.ExecuteAsync(new ApplyForInternshipRequest(userId, internship.Id)));
    }
}