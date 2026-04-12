using MassTransit;

namespace CoreService.Infrastructure.Saga;

public class OnboardingSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = default!;

    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = default!;
    public string CandidateEmail { get; set; } = default!;
    public string? CorporateEmail { get; set; }
    public string? FaultReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

