namespace Contracts.Events;

public record EmailSendingFailedEvent(
    Guid ApplicationId,
    string Reason);

