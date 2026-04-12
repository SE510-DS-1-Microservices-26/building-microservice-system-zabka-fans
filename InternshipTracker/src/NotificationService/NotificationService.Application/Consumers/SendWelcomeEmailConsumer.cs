using Contracts.Commands;
using Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace NotificationService.Application.Consumers;

public class SendWelcomeEmailConsumer : IConsumer<SendWelcomeEmailCommand>
{
    private readonly ILogger<SendWelcomeEmailConsumer> _logger;

    public SendWelcomeEmailConsumer(ILogger<SendWelcomeEmailConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendWelcomeEmailCommand> context)
    {
        var cmd = context.Message;

        _logger.LogInformation(
            "Sending welcome email for application {ApplicationId} to {CandidateEmail} (corporate: {CorporateEmail})",
            cmd.ApplicationId, cmd.CandidateEmail, cmd.CorporateEmail);

        try
        {
            // Simulate email dispatch (log-only for now; real SendGrid integration later)
            await Task.Delay(300, context.CancellationToken);

            _logger.LogInformation(
                "Welcome email sent successfully for application {ApplicationId}", cmd.ApplicationId);

            await context.Publish(new EmailSentEvent(cmd.ApplicationId), context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send welcome email for application {ApplicationId}", cmd.ApplicationId);

            await context.Publish(
                new EmailSendingFailedEvent(cmd.ApplicationId, ex.Message),
                context.CancellationToken);
        }
    }
}

