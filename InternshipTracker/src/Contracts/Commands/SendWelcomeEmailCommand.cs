namespace Contracts.Commands;

public record SendWelcomeEmailCommand(
    Guid ApplicationId,
    string CandidateEmail,
    string CorporateEmail);

