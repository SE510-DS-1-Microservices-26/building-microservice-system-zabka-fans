using Contracts.Events;
using CoreService.Application.Interfaces.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoreService.Infrastructure.Messaging.Consumers;

public class CorporateEmailSyncConsumer : IConsumer<CorporateEmailAddedEvent>
{
    private readonly IUserCoreRepository _userCoreRepository;
    private readonly ILogger<CorporateEmailSyncConsumer> _logger;

    public CorporateEmailSyncConsumer(
        IUserCoreRepository userCoreRepository,
        ILogger<CorporateEmailSyncConsumer> logger)
    {
        _userCoreRepository = userCoreRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CorporateEmailAddedEvent> context)
    {
        var msg = context.Message;

        var user = await _userCoreRepository.GetByIdAsync(msg.CandidateId, context.CancellationToken);
        if (user == null)
        {
            _logger.LogWarning("UserCore {CandidateId} not found for corporate email sync — skipping",
                msg.CandidateId);
            return;
        }

        user.SetCorporateEmail(msg.CorporateEmail);
        await _userCoreRepository.UnitOfWork.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Corporate email synced to UserCore {CandidateId}", msg.CandidateId);
    }
}

