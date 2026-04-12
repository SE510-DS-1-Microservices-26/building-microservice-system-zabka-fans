namespace Contracts.Commands;

public record FaultApplicationEnrollmentCommand(
    Guid ApplicationId,
    string Reason);

