using CoreService.Application.DTOs.Requests;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Application.Factories;
using CoreService.Application.UseCases;
using CoreService.Domain.Entities;
using CoreService.Domain.Enums;
using CoreService.Domain.Interfaces;
using NSubstitute;

namespace InternshipTracker.Tests;

public class ApplyForInternshipUseCaseTests
{
    [Test]
    public async Task ApplyForInternship_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var internshipId = Guid.NewGuid();

        var user = new UserCore(userId, "Test User", CandidateLevel.Junior);
        var internship = new Internship(internshipId, "Test Internship", 10, CandidateLevel.Junior);

        var userCoreRepo = Substitute.For<IUserCoreRepository>();
        userCoreRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var internshipRepo = Substitute.For<IInternshipRepository>();
        internshipRepo.GetByIdAsync(internshipId, Arg.Any<CancellationToken>()).Returns(internship);

        var applicationRepo = Substitute.For<IInternshipApplicationRepository>();

        var duplicationChecker = Substitute.For<IDuplicateApplicationChecker>();
        duplicationChecker
            .HasAppliedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var factory = new InternshipApplicationFactory(duplicationChecker);

        var useCase = new ApplyForInternshipUseCase(
            internshipRepo, applicationRepo, factory, userCoreRepo);

        // Act
        var result = await useCase.ExecuteAsync(new ApplyForInternshipRequest(userId, internshipId));

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Status, Is.EqualTo(ApplicationStatus.Pending));
        await applicationRepo
            .Received(1)
            .AddAsync(Arg.Any<InternshipApplication>(), Arg.Any<CancellationToken>());
    }
}