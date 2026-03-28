using Contracts.Users;
using CoreService.Application.Interfaces.Repositories;
using CoreService.Domain.Entities;
using CoreService.Domain.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreService.Infrastructure.Messaging.Consumers;

public class UserDbMessageConsumer : IConsumer<UserCreatedEvent>, IConsumer<UserDeletedEvent>
{
    private readonly ILogger<UserDbMessageConsumer> _logger;
    private readonly IUserCoreRepository _userCoreRepository;

    public UserDbMessageConsumer(ILogger<UserDbMessageConsumer> logger, IUserCoreRepository userCoreRepository)
    {
        _logger = logger;
        _userCoreRepository = userCoreRepository;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;

        try
        {
            var existing = await _userCoreRepository.GetByIdAsync(message.Id, context.CancellationToken);
            if (existing != null)
            {
                _logger.LogWarning("UserCore {UserId} already exists — skipping creation", message.Id);
                return;
            }

            if (!Enum.TryParse<CandidateLevel>(message.Level, out var level))
            {
                _logger.LogError("Invalid CandidateLevel '{Level}' received for user {UserId}", message.Level, message.Id);
                return;
            }

            var user = new UserCore(message.Id, message.Name, level);
            var added = await _userCoreRepository.AddAsync(user, context.CancellationToken);

            if (added == null)
            {
                _logger.LogError("Repository returned null after AddAsync for UserCore {UserId} — entity was not tracked", message.Id);
                return;
            }

            await _userCoreRepository.UnitOfWork.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation("UserCore {UserId} successfully synced to core database", message.Id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while persisting UserCore {UserId} — possible constraint violation", message.Id);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Consume of UserCreatedEvent was cancelled for user {UserId}", message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while consuming UserCreatedEvent for user {UserId}", message.Id);
        }
    }

    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        var message = context.Message;

        try
        {
            var user = await _userCoreRepository.GetByIdAsync(message.UserId, context.CancellationToken);
            if (user == null)
            {
                _logger.LogWarning("UserCore {UserId} not found for deletion — skipping", message.UserId);
                return;
            }

            var deleted = await _userCoreRepository.DeleteAsync(user, context.CancellationToken);
            if (!deleted)
            {
                _logger.LogError("Repository returned false after DeleteAsync for UserCore {UserId} — entity may have already been removed", message.UserId);
                return;
            }

            await _userCoreRepository.UnitOfWork.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation("UserCore {UserId} deleted from core database (applications cascade-deleted)", message.UserId);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while deleting UserCore {UserId} — cascade or constraint failure", message.UserId);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Consume of UserDeletedEvent was cancelled for user {UserId}", message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while consuming UserDeletedEvent for user {UserId}", message.UserId);
        }
    }
}
