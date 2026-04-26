using Contracts.Commands;
using Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ITProvisionService.Infrastructure.Consumers;

public class ProvisionCorporateAccountConsumer : IConsumer<ProvisionCorporateAccountCommand>
{
    private readonly ILogger<ProvisionCorporateAccountConsumer> _logger;

    public ProvisionCorporateAccountConsumer(ILogger<ProvisionCorporateAccountConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProvisionCorporateAccountCommand> context)
    {
        var cmd = context.Message;

        _logger.LogInformation(
            "Provisioning corporate account for candidate {CandidateId} ({CandidateName}), application {ApplicationId}",
            cmd.CandidateId, cmd.CandidateName, cmd.ApplicationId);

        try
        {
            // Simulate account creation — derive a corporate email from the candidate name
            var normalisedName = cmd.CandidateName
                .Trim()
                .Replace(" ", ".", StringComparison.OrdinalIgnoreCase)
                .ToLowerInvariant();
            var corporateEmail = $"{normalisedName}@corp.internship.com";

            // Simulate latency
            await Task.Delay(500, context.CancellationToken);

            _logger.LogInformation(
                "Corporate account provisioned: {CorporateEmail} for application {ApplicationId}",
                corporateEmail, cmd.ApplicationId);

            await context.Publish(new AccountProvisionedEvent(cmd.ApplicationId, corporateEmail),
                context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to provision corporate account for application {ApplicationId}", cmd.ApplicationId);

            await context.Publish(new AccountProvisioningFailedEvent(cmd.ApplicationId, ex.Message),
                context.CancellationToken);
        }
    }
}

