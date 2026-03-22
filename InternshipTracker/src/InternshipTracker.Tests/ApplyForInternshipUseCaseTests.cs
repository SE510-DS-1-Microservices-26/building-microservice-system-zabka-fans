using InternshipTracker.Application.DTOs.Requests;
using InternshipTracker.Application.Interfaces.Repositories;
using InternshipTracker.Application.UseCases;
using InternshipTracker.Domain.Entities;
using InternshipTracker.Domain.Enums;
using InternshipTracker.Domain.Factories;
using InternshipTracker.Domain.Interfaces;
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

        var user = new User(userId, "Test User", CandidateLevel.Junior);
        var internship = new Internship(internshipId, "Test Internship", 10, CandidateLevel.Junior);

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var internshipRepo = Substitute.For<IInternshipRepository>();
        internshipRepo.GetByIdAsync(internshipId, Arg.Any<CancellationToken>()).Returns(internship);

        var applicationRepo = Substitute.For<IInternshipApplicationRepository>();

        var duplicationChecker = Substitute.For<IDuplicateApplicationChecker>();
        duplicationChecker
            .HasAppliedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var factory = new InternshipApplicationFactory(duplicationChecker);

        var useCase = new ApplyForInternshipUseCase(userRepo, internshipRepo, applicationRepo, factory);

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