using CoreService.Domain.Entities;
using CoreService.Infrastructure.Messaging.Messages;
using CoreService.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreService.Infrastructure.Messaging.Consumers;

public class UserDbMessageConsumer : IConsumer<UserCreatedDbMessage>, IConsumer<UserDeletedDbMessage>
{
    private readonly ILogger<UserDbMessageConsumer> _logger;
    private readonly CoreDbContext _dbContext;

    public UserDbMessageConsumer(ILogger<UserDbMessageConsumer> logger, CoreDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<UserCreatedDbMessage> context)
    {
        var message = context.Message;

        var exists = await _dbContext.Users.AnyAsync(u => u.Id == message.Id);
        if (exists)
        {
            _logger.LogWarning("UserCore with ID {UserId} already exists, skipping creation", message.Id);
            return;
        }

        var user = new UserCore(message.Id, message.Name, message.Level);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("UserCore with ID {UserId} synced to core database", message.Id);
    }

    public async Task Consume(ConsumeContext<UserDeletedDbMessage> context)
    {
        var message = context.Message;

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == message.UserId);
        if (user == null)
        {
            _logger.LogWarning("UserCore with ID {UserId} not found for deletion", message.UserId);
            return;
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("UserCore with ID {UserId} deleted from core database (cascade will remove applications)", message.UserId);
    }
}