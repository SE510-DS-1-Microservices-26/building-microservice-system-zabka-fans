using Contracts.Commands;
using Contracts.Events;
using MassTransit;

namespace CoreService.Infrastructure.Saga;

public class OnboardingSagaStateMachine : MassTransitStateMachine<OnboardingSagaState>
{
    // ── States ──
    public State ProvisioningIT { get; private set; } = default!;
    public State SendingNotification { get; private set; } = default!;
    public State Completed { get; private set; } = default!;
    public State Faulted { get; private set; } = default!;

    // ── Events ──
    public Event<OnboardingStartedEvent> OnboardingStarted { get; private set; } = default!;
    public Event<AccountProvisionedEvent> AccountProvisioned { get; private set; } = default!;
    public Event<AccountProvisioningFailedEvent> AccountProvisioningFailed { get; private set; } = default!;
    public Event<EmailSentEvent> EmailSent { get; private set; } = default!;
    public Event<EmailSendingFailedEvent> EmailSendingFailed { get; private set; } = default!;

    public OnboardingSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // ── Correlation ──
        Event(() => OnboardingStarted, e => e.CorrelateById(ctx => ctx.Message.ApplicationId));
        Event(() => AccountProvisioned, e => e.CorrelateById(ctx => ctx.Message.ApplicationId));
        Event(() => AccountProvisioningFailed, e => e.CorrelateById(ctx => ctx.Message.ApplicationId));
        Event(() => EmailSent, e => e.CorrelateById(ctx => ctx.Message.ApplicationId));
        Event(() => EmailSendingFailed, e => e.CorrelateById(ctx => ctx.Message.ApplicationId));

        // ── Phase 1: Initiation → IT Provisioning ──
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

        // ── Phase 2: IT Provisioning result ──
        During(ProvisioningIT,
            // 2a — Success → send welcome email
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

            // 2b — Failure → compensate
            When(AccountProvisioningFailed)
                .Then(ctx =>
                {
                    ctx.Saga.FaultReason = ctx.Message.Reason;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new RevertApplicationStatusCommand(ctx.Saga.CorrelationId))
                .TransitionTo(Faulted)
        );

        // ── Phase 3: Notification result ──
        During(SendingNotification,
            // 3a — Success → finalize enrollment
            When(EmailSent)
                .Then(ctx => ctx.Saga.UpdatedAt = DateTime.UtcNow)
                .Publish(ctx => new FinalizeEnrollmentCommand(ctx.Saga.CorrelationId))
                .TransitionTo(Completed)
                .Finalize(),

            // 3b — Failure → fault (IT account stays, only email failed)
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
        );

        SetCompletedWhenFinalized();
    }
}

