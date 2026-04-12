using Contracts.Commands;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoreService.Infrastructure.Messaging.Consumers;

public class RevertApplicationStatusConsumer : IConsumer<RevertApplicationStatusCommand>
{
    private readonly IInternshipApplicationRepository _appRepository;
    private readonly ILogger<RevertApplicationStatusConsumer> _logger;

    public RevertApplicationStatusConsumer(
        IInternshipApplicationRepository appRepository,
        ILogger<RevertApplicationStatusConsumer> logger)
    {
        _appRepository = appRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RevertApplicationStatusCommand> context)
    {
        var applicationId = context.Message.ApplicationId;

        var application = await _appRepository.GetWithDetailsAsync(applicationId, context.CancellationToken);
        if (application == null)
        {
            _logger.LogError("Application {ApplicationId} not found for status revert", applicationId);
            return;
        }

        if (application.Status != ApplicationStatus.Enrolling)
        {
            _logger.LogWarning("Application {ApplicationId} is in status {Status}, expected Enrolling — skipping revert",
                applicationId, application.Status);
            return;
        }

        application.RevertToAccepted();
        await _appRepository.UnitOfWork.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Application {ApplicationId} reverted to Accepted (saga compensation)", applicationId);
    }
}

