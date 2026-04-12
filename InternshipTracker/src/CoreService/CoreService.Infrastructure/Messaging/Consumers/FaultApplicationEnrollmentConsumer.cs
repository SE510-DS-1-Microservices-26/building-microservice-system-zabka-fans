using Contracts.Commands;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoreService.Infrastructure.Messaging.Consumers;

public class FaultApplicationEnrollmentConsumer : IConsumer<FaultApplicationEnrollmentCommand>
{
    private readonly IInternshipApplicationRepository _appRepository;
    private readonly ILogger<FaultApplicationEnrollmentConsumer> _logger;

    public FaultApplicationEnrollmentConsumer(
        IInternshipApplicationRepository appRepository,
        ILogger<FaultApplicationEnrollmentConsumer> logger)
    {
        _appRepository = appRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FaultApplicationEnrollmentCommand> context)
    {
        var applicationId = context.Message.ApplicationId;

        var application = await _appRepository.GetWithDetailsAsync(applicationId, context.CancellationToken);
        if (application == null)
        {
            _logger.LogError("Application {ApplicationId} not found for enrollment fault", applicationId);
            return;
        }

        if (application.Status != ApplicationStatus.Enrolling)
        {
            _logger.LogWarning("Application {ApplicationId} is in status {Status}, expected Enrolling — skipping fault",
                applicationId, application.Status);
            return;
        }

        application.MarkAsEnrolledNotificationFault();
        await _appRepository.UnitOfWork.SaveChangesAsync(context.CancellationToken);

        _logger.LogCritical(
            "Application {ApplicationId} marked as EnrolledNotificationFault — email dispatch failed: {Reason}",
            applicationId, context.Message.Reason);
    }
}

