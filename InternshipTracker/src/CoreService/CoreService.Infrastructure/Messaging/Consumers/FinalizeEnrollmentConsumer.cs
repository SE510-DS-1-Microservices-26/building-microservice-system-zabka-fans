using Contracts.Commands;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoreService.Infrastructure.Messaging.Consumers;

public class FinalizeEnrollmentConsumer : IConsumer<FinalizeEnrollmentCommand>
{
    private readonly IInternshipApplicationRepository _appRepository;
    private readonly ILogger<FinalizeEnrollmentConsumer> _logger;

    public FinalizeEnrollmentConsumer(
        IInternshipApplicationRepository appRepository,
        ILogger<FinalizeEnrollmentConsumer> logger)
    {
        _appRepository = appRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FinalizeEnrollmentCommand> context)
    {
        var applicationId = context.Message.ApplicationId;

        var application = await _appRepository.GetWithDetailsAsync(applicationId, context.CancellationToken);
        if (application == null)
        {
            _logger.LogError("Application {ApplicationId} not found for enrollment finalization", applicationId);
            return;
        }

        if (application.Status != ApplicationStatus.Enrolling)
        {
            _logger.LogWarning("Application {ApplicationId} is in status {Status}, expected Enrolling — skipping finalize",
                applicationId, application.Status);
            return;
        }

        application.MarkAsEnrolled();
        await _appRepository.UnitOfWork.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Application {ApplicationId} finalized as Enrolled (saga complete)", applicationId);
    }
}

