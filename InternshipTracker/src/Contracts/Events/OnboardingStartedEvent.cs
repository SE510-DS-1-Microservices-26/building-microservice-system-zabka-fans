namespace Contracts.Events;

public record OnboardingStartedEvent(
    Guid ApplicationId,
    Guid CandidateId,
    string CandidateName,
    string CandidateEmail);

