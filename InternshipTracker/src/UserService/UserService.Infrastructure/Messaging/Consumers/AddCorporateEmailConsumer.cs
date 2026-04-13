using Contracts.Commands;
using Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Messaging.Consumers;

public class AddCorporateEmailConsumer : IConsumer<AddCorporateEmailCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AddCorporateEmailConsumer> _logger;

    public AddCorporateEmailConsumer(
        IUserRepository userRepository,
        ILogger<AddCorporateEmailConsumer> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AddCorporateEmailCommand> context)
    {
        var cmd = context.Message;

        var user = await _userRepository.GetByIdAsync(cmd.CandidateId, context.CancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {CandidateId} not found — publishing failure event", cmd.CandidateId);
            await context.Publish(new CorporateEmailAdditionFailedEvent(
                cmd.ApplicationId,
                $"User with ID {cmd.CandidateId} was not found."));
            return;
        }

        user.SetCorporateEmail(cmd.CorporateEmail);
        await _userRepository.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Corporate email {CorporateEmail} set for user {CandidateId}",
            cmd.CorporateEmail, cmd.CandidateId);

        await context.Publish(new CorporateEmailAddedEvent(
            cmd.ApplicationId,
            cmd.CandidateId,
            cmd.CorporateEmail));
    }
}

