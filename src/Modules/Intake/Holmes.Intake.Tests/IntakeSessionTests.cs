using FluentAssertions;
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

        session.Status.Should().Be(IntakeSessionStatus.Invited);
        session.PolicySnapshot.Should().Be(snapshot);
        session.DomainEvents.Should().ContainSingle(e => e is IntakeSessionInvited);
    }

    [Fact]
    public void Start_MovesSessionToInProgress()
    {
        var session = CreateInvitedSession();

        session.Start(DateTimeOffset.UtcNow, "ios-17");

        session.Status.Should().Be(IntakeSessionStatus.InProgress);
        session.DomainEvents.Should().Contain(e => e is IntakeSessionStarted);
    }

    [Fact]
    public void Submit_RequiresConsentAndAnswers()
    {
        var session = CreateInvitedSession();
        session.Start(DateTimeOffset.UtcNow, null);
        var answers = IntakeAnswersSnapshot.Create("schema", "hash", "cipher", DateTimeOffset.UtcNow);

        session.Invoking(s => s.Submit(DateTimeOffset.UtcNow))
            .Should()
            .Throw<InvalidOperationException>();

        session.SaveProgress(answers);
        session.Invoking(s => s.Submit(DateTimeOffset.UtcNow))
            .Should()
            .Throw<InvalidOperationException>();

        var artifact = ConsentArtifactPointer.Create(UlidId.NewUlid(), "application/pdf", 1024, "hash", "SHA256",
            "schema", DateTimeOffset.UtcNow);
        session.CaptureConsent(artifact);

        session.Invoking(s => s.Submit(DateTimeOffset.UtcNow)).Should().NotThrow();
        session.Status.Should().Be(IntakeSessionStatus.AwaitingReview);
    }

    [Fact]
    public void AcceptSubmission_FinalizesSession()
    {
        var session = CreateSubmittedSession();

        session.AcceptSubmission(DateTimeOffset.UtcNow);

        session.Status.Should().Be(IntakeSessionStatus.Submitted);
        session.DomainEvents.Should().Contain(e => e is IntakeSubmissionAccepted);
    }

    [Fact]
    public void Supersede_DisallowedAfterSubmitted()
    {
        var session = CreateSubmittedSession();
        session.AcceptSubmission(DateTimeOffset.UtcNow);

        session.Invoking(s => s.Supersede(UlidId.NewUlid(), DateTimeOffset.UtcNow))
            .Should()
            .Throw<InvalidOperationException>();
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