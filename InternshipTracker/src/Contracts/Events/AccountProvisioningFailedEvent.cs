namespace Contracts.Events;

public record AccountProvisioningFailedEvent(
    Guid ApplicationId,
    string Reason);

