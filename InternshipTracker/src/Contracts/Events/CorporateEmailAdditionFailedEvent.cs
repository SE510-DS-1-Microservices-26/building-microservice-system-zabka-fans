namespace Contracts.Events;

public record CorporateEmailAdditionFailedEvent(
    Guid ApplicationId,
    string Reason);

