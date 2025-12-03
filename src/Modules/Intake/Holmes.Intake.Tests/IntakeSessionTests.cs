using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.Events;
using Holmes.Intake.Domain.ValueObjects;

namespace Holmes.Intake.Tests;

public class IntakeSessionTests
{
    [Test]
    public void Invite_CreatesSessionWithInitialState()
    {
        var snapshot = PolicySnapshot.Create("snapshot-1", "schema-1", DateTimeOffset.UtcNow);
        var session = IntakeSession.Invite(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            snapshot,
            "resume-token",
            DateTimeOffset.UtcNow,
            TimeSpan.FromDays(3));

        Assert.Multiple(() =>
        {
            Assert.That(session.Status, Is.EqualTo(IntakeSessionStatus.Invited));
            Assert.That(session.PolicySnapshot, Is.EqualTo(snapshot));
            Assert.That(session.DomainEvents.Count(e => e is IntakeSessionInvited), Is.EqualTo(1));
        });
    }

    [Test]
    public void Start_MovesSessionToInProgress()
    {
        var session = CreateInvitedSession();

        session.Start(DateTimeOffset.UtcNow, "ios-17");

        Assert.Multiple(() =>
        {
            Assert.That(session.Status, Is.EqualTo(IntakeSessionStatus.InProgress));
            Assert.That(session.DomainEvents.Any(e => e is IntakeSessionStarted), Is.True);
        });
    }

    [Test]
    public void Submit_RequiresConsentAndAnswers()
    {
        var session = CreateInvitedSession();
        session.Start(DateTimeOffset.UtcNow, null);
        var answers = IntakeAnswersSnapshot.Create("schema", "hash", "cipher", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => session.Submit(DateTimeOffset.UtcNow));

        session.SaveProgress(answers);
        Assert.Throws<InvalidOperationException>(() => session.Submit(DateTimeOffset.UtcNow));

        var artifact = ConsentArtifactPointer.Create(UlidId.NewUlid(), "application/pdf", 1024, "hash", "SHA256",
            "schema", DateTimeOffset.UtcNow);
        session.CaptureConsent(artifact);

        Assert.DoesNotThrow(() => session.Submit(DateTimeOffset.UtcNow));
        Assert.That(session.Status, Is.EqualTo(IntakeSessionStatus.AwaitingReview));
    }

    [Test]
    public void AcceptSubmission_FinalizesSession()
    {
        var session = CreateSubmittedSession();

        session.AcceptSubmission(DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(session.Status, Is.EqualTo(IntakeSessionStatus.Submitted));
            Assert.That(session.DomainEvents.Any(e => e is IntakeSubmissionAccepted), Is.True);
        });
    }

    [Test]
    public void Supersede_DisallowedAfterSubmitted()
    {
        var session = CreateSubmittedSession();
        session.AcceptSubmission(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            session.Supersede(UlidId.NewUlid(), DateTimeOffset.UtcNow));
    }

    private static IntakeSession CreateInvitedSession()
    {
        var snapshot = PolicySnapshot.Create("snapshot-1", "schema-1", DateTimeOffset.UtcNow);
        return IntakeSession.Invite(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            snapshot,
            "resume-token",
            DateTimeOffset.UtcNow,
            TimeSpan.FromDays(7));
    }

    private static IntakeSession CreateSubmittedSession()
    {
        var session = CreateInvitedSession();
        session.Start(DateTimeOffset.UtcNow, null);
        var answers = IntakeAnswersSnapshot.Create("schema", "hash", "cipher", DateTimeOffset.UtcNow);
        session.SaveProgress(answers);
        var artifact = ConsentArtifactPointer.Create(UlidId.NewUlid(), "application/pdf", 1024, "hash", "SHA256",
            "schema", DateTimeOffset.UtcNow);
        session.CaptureConsent(artifact);
        session.Submit(DateTimeOffset.UtcNow);
        return session;
    }
}