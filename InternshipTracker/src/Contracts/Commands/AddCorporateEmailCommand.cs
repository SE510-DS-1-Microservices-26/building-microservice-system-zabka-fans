namespace Contracts.Commands;

public record AddCorporateEmailCommand(
    Guid ApplicationId,
    Guid CandidateId,
    string CorporateEmail);

