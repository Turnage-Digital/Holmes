using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.Events;
using Holmes.Intake.Domain.ValueObjects;
using Xunit;

namespace Holmes.Intake.Tests;

public class IntakeSessionTests
{
    [Fact]
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

        Assert.Equal(IntakeSessionStatus.Invited, session.Status);
        Assert.Equal(snapshot, session.PolicySnapshot);
        Assert.Single(session.DomainEvents, e => e is IntakeSessionInvited);
    }

    [Fact]
    public void Start_MovesSessionToInProgress()
    {
        var session = CreateInvitedSession();

        session.Start(DateTimeOffset.UtcNow, "ios-17");

        Assert.Equal(IntakeSessionStatus.InProgress, session.Status);
        Assert.Contains(session.DomainEvents, e => e is IntakeSessionStarted);
    }

    [Fact]
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

        var exception = Record.Exception(() => session.Submit(DateTimeOffset.UtcNow));
        Assert.Null(exception);
        Assert.Equal(IntakeSessionStatus.AwaitingReview, session.Status);
    }

    [Fact]
    public void AcceptSubmission_FinalizesSession()
    {
        var session = CreateSubmittedSession();

        session.AcceptSubmission(DateTimeOffset.UtcNow);

        Assert.Equal(IntakeSessionStatus.Submitted, session.Status);
        Assert.Contains(session.DomainEvents, e => e is IntakeSubmissionAccepted);
    }

    [Fact]
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