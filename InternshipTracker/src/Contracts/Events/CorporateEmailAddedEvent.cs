namespace Contracts.Events;

public record CorporateEmailAddedEvent(
    Guid ApplicationId,
    Guid CandidateId,
    string CorporateEmail);

