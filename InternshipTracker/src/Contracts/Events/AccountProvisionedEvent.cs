namespace Contracts.Events;

public record AccountProvisionedEvent(
    Guid ApplicationId,
    string CorporateEmail);

