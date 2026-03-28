using CoreService.Infrastructure.Messaging.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CoreService.Infrastructure.Messaging.Consumers;

public class UserDbMessageFaultConsumer :
    IConsumer<Fault<UserCreatedDbMessage>>,
    IConsumer<Fault<UserDeletedDbMessage>>
{
    ILogger<UserDbMessageFaultConsumer> _logger;

    public UserDbMessageFaultConsumer(ILogger<UserDbMessageFaultConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Fault<UserCreatedDbMessage>> context) => LogFault(context);

    public Task Consume(ConsumeContext<Fault<UserDeletedDbMessage>> context) => LogFault(context);

    private Task LogFault<T>(ConsumeContext<Fault<T>> context)
        where T : class
    {
        var original = context.Message.Message;
        var exceptions = context.Message.Exceptions
            .Select(x => $"{x.ExceptionType}: {x.Message}")
            .ToArray();

        _logger.LogError(
            "Fault handling message {MessageType} with content {@OriginalMessage}. Exceptions: {Exceptions}",
            typeof(T).Name,
            original,
            exceptions
        );

        return Task.CompletedTask;
    }
}