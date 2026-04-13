using Contracts.Commands;
using Contracts.Events;
using MassTransit;

namespace CoreService.Infrastructure.Saga;

public class OnboardingSagaStateMachine : MassTransitStateMachine<OnboardingSagaState>
{
    public State ProvisioningIT { get; private set; } = default!;
    public State SendingNotification { get; private set; } = default!;
    public State Completed { get; private set; } = default!;
    public State Faulted { get; private set; } = default!;

    public Event<OnboardingStartedEvent> OnboardingStarted { get; private set; } = default!;
    public Event<AccountProvisionedEvent> AccountProvisioned { get; private set; } = default!;
    public Event<AccountProvisioningFailedEvent> AccountProvisioningFailed { get; private set; } = default!;
    public Event<EmailSentEvent> EmailSent { get; private set; } = default!;
    public Event<EmailSendingFailedEvent> EmailSendingFailed { get; private set; } = default!;

    public OnboardingSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OnboardingStarted, e => e.CorrelateById(ctx => ctx.Message.ApplicationId));
        Event(() => AccountProvisioned, e => e.CorrelateById(ctx => ctx.Message.ApplicationId));
        Event(() => AccountProvisioningFailed, e => e.CorrelateById(ctx => ctx.Message.ApplicationId));
        Event(() => EmailSent, e => e.CorrelateById(ctx => ctx.Message.ApplicationId));
        Event(() => EmailSendingFailed, e => e.CorrelateById(ctx => ctx.Message.ApplicationId));

        Initially(
            When(OnboardingStarted)
                .Then(ctx =>
                {
                    ctx.Saga.CandidateId = ctx.Message.CandidateId;
                    ctx.Saga.CandidateName = ctx.Message.CandidateName;
                    ctx.Saga.CandidateEmail = ctx.Message.CandidateEmail;
                    ctx.Saga.CreatedAt = DateTime.UtcNow;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new ProvisionCorporateAccountCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.CandidateId,
                    ctx.Saga.CandidateName,
                    ctx.Saga.CandidateEmail))
                .TransitionTo(ProvisioningIT)
        );

        During(ProvisioningIT,
            When(AccountProvisioned)
                .Then(ctx =>
                {
                    ctx.Saga.CorporateEmail = ctx.Message.CorporateEmail;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new SendWelcomeEmailCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.CandidateEmail,
                    ctx.Saga.CorporateEmail!))
                .TransitionTo(SendingNotification),

            When(AccountProvisioningFailed)
                .Then(ctx =>
                {
                    ctx.Saga.FaultReason = ctx.Message.Reason;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new RevertApplicationStatusCommand(ctx.Saga.CorrelationId))
                .TransitionTo(Faulted)
                .Finalize()
        );

        During(SendingNotification,
            When(EmailSent)
                .Then(ctx => ctx.Saga.UpdatedAt = DateTime.UtcNow)
                .Publish(ctx => new FinalizeEnrollmentCommand(ctx.Saga.CorrelationId))
                .TransitionTo(Completed)
                .Finalize(),

            // IT account was already created, so we can't fully roll back — record the fault
            When(EmailSendingFailed)
                .Then(ctx =>
                {
                    ctx.Saga.FaultReason = ctx.Message.Reason;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new FaultApplicationEnrollmentCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Message.Reason))
                .TransitionTo(Faulted)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
