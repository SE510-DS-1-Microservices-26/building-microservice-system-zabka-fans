namespace Contracts.Commands;

public record ProvisionCorporateAccountCommand(
    Guid ApplicationId,
    Guid CandidateId,
    string CandidateName,
    string CandidateEmail);

