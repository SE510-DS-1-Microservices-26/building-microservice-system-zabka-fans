using Contracts.Commands;
using Contracts.Events;
using CoreService.Infrastructure.Saga;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace InternshipTracker.Tests;

public class OnboardingSagaTests : IAsyncDisposable
{
    private static readonly Guid ApplicationId = Guid.NewGuid();
    private static readonly Guid CandidateId = Guid.NewGuid();
    private const string CandidateName = "Alice Smith";
    private const string CandidateEmail = "alice.smith@example.com";
    private const string CorporateEmail = "alice.smith@corp.internship.com";
    private const string ProvisioningFailureReason = "Username already exists";
    private const string EmailFailureReason = "SendGrid API unavailable";

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

    // OnboardingStarted

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

    // AccountProvisioned

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

    // AccountProvisioningFailed

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

    // EmailSent

    [Test]
    public async Task EmailSent_FinalizesSaga_SagaInstanceRemoved()
    {
        await DriveToSendingNotification();

        await _harness.Bus.Publish(new EmailSentEvent(ApplicationId));

        Assert.That(await _harness.Published.Any<FinalizeEnrollmentCommand>(
            x => x.Context.Message.ApplicationId == ApplicationId), Is.True);

        // saga is removed from the repository after finalization
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

    // EmailSendingFailed

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
