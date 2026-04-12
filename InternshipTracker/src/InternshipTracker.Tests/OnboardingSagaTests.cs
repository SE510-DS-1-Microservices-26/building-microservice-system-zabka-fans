using Contracts.Commands;
using Contracts.Events;
using CoreService.Infrastructure.Saga;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace InternshipTracker.Tests;

/// <summary>
/// Tests the <see cref="OnboardingSagaStateMachine"/> using the MassTransit in-memory test harness.
/// Verifies every state transition and the commands published at each phase.
/// </summary>
public class OnboardingSagaTests : IAsyncDisposable
{
    // ── Shared constants (no magic values) ──────────────────────────────────

    private static readonly Guid ApplicationId = Guid.NewGuid();
    private static readonly Guid CandidateId = Guid.NewGuid();
    private const string CandidateName = "Alice Smith";
    private const string CandidateEmail = "alice.smith@example.com";
    private const string CorporateEmail = "alice.smith@corp.internship.com";
    private const string ProvisioningFailureReason = "Username already exists";
    private const string EmailFailureReason = "SendGrid API unavailable";

    // ── Harness ─────────────────────────────────────────────────────────────

    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<OnboardingSagaStateMachine, OnboardingSagaState> _sagaHarness = null!;

    [SetUp]
    public async Task SetUp()
    {
        _provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<OnboardingSagaStateMachine, OnboardingSagaState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();

        _sagaHarness = _harness.GetSagaStateMachineHarness<OnboardingSagaStateMachine, OnboardingSagaState>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private static OnboardingStartedEvent CreateOnboardingStartedEvent() =>
        new(ApplicationId, CandidateId, CandidateName, CandidateEmail);

    private async Task DriveToProvisioningIT()
    {
        await _harness.Bus.Publish(CreateOnboardingStartedEvent());
        Assert.That(await _sagaHarness.Exists(ApplicationId, s => s.ProvisioningIT), Is.Not.Null);
    }

    private async Task DriveToSendingNotification()
    {
        await DriveToProvisioningIT();
        await _harness.Bus.Publish(new AccountProvisionedEvent(ApplicationId, CorporateEmail));
        Assert.That(await _sagaHarness.Exists(ApplicationId, s => s.SendingNotification), Is.Not.Null);
    }

    // ── Phase 1: Initiation → ProvisioningIT ────────────────────────────────

    [Test]
    public async Task OnboardingStarted_TransitionsToProvisioningIT()
    {
        await _harness.Bus.Publish(CreateOnboardingStartedEvent());

        Assert.That(
            await _sagaHarness.Exists(ApplicationId, s => s.ProvisioningIT),
            Is.Not.Null);
    }

    [Test]
    public async Task OnboardingStarted_StoresCandidateData()
    {
        await DriveToProvisioningIT();

        var saga = _sagaHarness.Sagas.ContainsInState(ApplicationId, _sagaHarness.StateMachine, s => s.ProvisioningIT);
        Assert.That(saga, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(saga!.CandidateId, Is.EqualTo(CandidateId));
            Assert.That(saga.CandidateName, Is.EqualTo(CandidateName));
            Assert.That(saga.CandidateEmail, Is.EqualTo(CandidateEmail));
        });
    }

    [Test]
    public async Task OnboardingStarted_PublishesProvisionCommand()
    {
        await DriveToProvisioningIT();

        Assert.That(await _harness.Published.Any<ProvisionCorporateAccountCommand>(
            x => x.Context.Message.ApplicationId == ApplicationId
                 && x.Context.Message.CandidateEmail == CandidateEmail), Is.True);
    }

    // ── Phase 2a: IT Success → SendingNotification ──────────────────────────

    [Test]
    public async Task AccountProvisioned_TransitionsToSendingNotification()
    {
        await DriveToSendingNotification();

        Assert.That(
            await _sagaHarness.Exists(ApplicationId, s => s.SendingNotification),
            Is.Not.Null);
    }

    [Test]
    public async Task AccountProvisioned_StoresCorporateEmail()
    {
        await DriveToSendingNotification();

        var saga = _sagaHarness.Sagas.ContainsInState(ApplicationId, _sagaHarness.StateMachine, s => s.SendingNotification);
        Assert.That(saga!.CorporateEmail, Is.EqualTo(CorporateEmail));
    }

    [Test]
    public async Task AccountProvisioned_PublishesSendWelcomeEmailCommand()
    {
        await DriveToSendingNotification();

        Assert.That(await _harness.Published.Any<SendWelcomeEmailCommand>(
            x => x.Context.Message.ApplicationId == ApplicationId
                 && x.Context.Message.CorporateEmail == CorporateEmail), Is.True);
    }

    // ── Phase 2b: IT Failure → Faulted (compensation) ──────────────────────

    [Test]
    public async Task AccountProvisioningFailed_TransitionsToFaulted()
    {
        await DriveToProvisioningIT();

        await _harness.Bus.Publish(new AccountProvisioningFailedEvent(ApplicationId, ProvisioningFailureReason));

        Assert.That(
            await _sagaHarness.Exists(ApplicationId, s => s.Faulted),
            Is.Not.Null);
    }

    [Test]
    public async Task AccountProvisioningFailed_StoresFaultReason()
    {
        await DriveToProvisioningIT();
        await _harness.Bus.Publish(new AccountProvisioningFailedEvent(ApplicationId, ProvisioningFailureReason));
        Assert.That(await _sagaHarness.Exists(ApplicationId, s => s.Faulted), Is.Not.Null);

        var saga = _sagaHarness.Sagas.ContainsInState(ApplicationId, _sagaHarness.StateMachine, s => s.Faulted);
        Assert.That(saga!.FaultReason, Is.EqualTo(ProvisioningFailureReason));
    }

    [Test]
    public async Task AccountProvisioningFailed_PublishesRevertCommand()
    {
        await DriveToProvisioningIT();
        await _harness.Bus.Publish(new AccountProvisioningFailedEvent(ApplicationId, ProvisioningFailureReason));

        Assert.That(await _harness.Published.Any<RevertApplicationStatusCommand>(
            x => x.Context.Message.ApplicationId == ApplicationId), Is.True);
    }

    // ── Phase 3a: Email Success → Completed ─────────────────────────────────

    [Test]
    public async Task EmailSent_FinalizesSaga_SagaInstanceRemoved()
    {
        await DriveToSendingNotification();

        await _harness.Bus.Publish(new EmailSentEvent(ApplicationId));

        // Verify finalize command was published — proves the saga reached the Completed branch.
        Assert.That(await _harness.Published.Any<FinalizeEnrollmentCommand>(
            x => x.Context.Message.ApplicationId == ApplicationId), Is.True);

        // After SetCompletedWhenFinalized the saga is removed from the in-memory repo.
        var remaining = _sagaHarness.Sagas.ContainsInState(
            ApplicationId, _sagaHarness.StateMachine, s => s.ProvisioningIT);
        Assert.That(remaining, Is.Null, "Saga should no longer be in any active state");
    }

    [Test]
    public async Task EmailSent_PublishesFinalizeEnrollmentCommand()
    {
        await DriveToSendingNotification();
        await _harness.Bus.Publish(new EmailSentEvent(ApplicationId));

        Assert.That(await _harness.Published.Any<FinalizeEnrollmentCommand>(
            x => x.Context.Message.ApplicationId == ApplicationId), Is.True);
    }

    // ── Phase 3b: Email Failure → Faulted ───────────────────────────────────

    [Test]
    public async Task EmailSendingFailed_TransitionsToFaulted()
    {
        await DriveToSendingNotification();

        await _harness.Bus.Publish(new EmailSendingFailedEvent(ApplicationId, EmailFailureReason));

        Assert.That(
            await _sagaHarness.Exists(ApplicationId, s => s.Faulted),
            Is.Not.Null);
    }

    [Test]
    public async Task EmailSendingFailed_StoresFaultReason()
    {
        await DriveToSendingNotification();
        await _harness.Bus.Publish(new EmailSendingFailedEvent(ApplicationId, EmailFailureReason));
        Assert.That(await _sagaHarness.Exists(ApplicationId, s => s.Faulted), Is.Not.Null);

        var saga = _sagaHarness.Sagas.ContainsInState(ApplicationId, _sagaHarness.StateMachine, s => s.Faulted);
        Assert.That(saga!.FaultReason, Is.EqualTo(EmailFailureReason));
    }

    [Test]
    public async Task EmailSendingFailed_PublishesFaultApplicationCommand()
    {
        await DriveToSendingNotification();
        await _harness.Bus.Publish(new EmailSendingFailedEvent(ApplicationId, EmailFailureReason));

        Assert.That(await _harness.Published.Any<FaultApplicationEnrollmentCommand>(
            x => x.Context.Message.ApplicationId == ApplicationId
                 && x.Context.Message.Reason == EmailFailureReason), Is.True);
    }
}



