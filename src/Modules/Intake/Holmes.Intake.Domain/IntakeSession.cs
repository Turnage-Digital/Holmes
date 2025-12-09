using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain.Events;
using Holmes.Intake.Domain.ValueObjects;

namespace Holmes.Intake.Domain;

public sealed class IntakeSession : AggregateRoot
{
    private IntakeSession()
    {
    }

    public UlidId Id { get; private set; }
    public UlidId OrderId { get; private set; }
    public UlidId SubjectId { get; private set; }
    public UlidId CustomerId { get; private set; }
    public IntakeSessionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastTouchedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public string ResumeToken { get; private set; } = string.Empty;
    public PolicySnapshot PolicySnapshot { get; private set; } = null!;
    public IntakeAnswersSnapshot? AnswersSnapshot { get; private set; }
    public ConsentArtifactPointer? ConsentArtifact { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public UlidId? SupersededBySessionId { get; private set; }

    public static IntakeSession Rehydrate(
        UlidId sessionId,
        UlidId orderId,
        UlidId subjectId,
        UlidId customerId,
        IntakeSessionStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset lastTouchedAt,
        DateTimeOffset expiresAt,
        string resumeToken,
        PolicySnapshot policySnapshot,
        IntakeAnswersSnapshot? answersSnapshot,
        ConsentArtifactPointer? consentArtifact,
        DateTimeOffset? submittedAt,
        DateTimeOffset? acceptedAt,
        string? cancellationReason,
        UlidId? supersededBySessionId
    )
    {
        return new IntakeSession
        {
            Id = sessionId,
            OrderId = orderId,
            SubjectId = subjectId,
            CustomerId = customerId,
            Status = status,
            CreatedAt = createdAt,
            LastTouchedAt = lastTouchedAt,
            ExpiresAt = expiresAt,
            ResumeToken = resumeToken,
            PolicySnapshot = policySnapshot,
            AnswersSnapshot = answersSnapshot,
            ConsentArtifact = consentArtifact,
            SubmittedAt = submittedAt,
            AcceptedAt = acceptedAt,
            CancellationReason = cancellationReason,
            SupersededBySessionId = supersededBySessionId
        };
    }

    public static IntakeSession Invite(
        UlidId sessionId,
        UlidId orderId,
        UlidId subjectId,
        UlidId customerId,
        PolicySnapshot policySnapshot,
        string resumeToken,
        DateTimeOffset invitedAt,
        TimeSpan timeToLive
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resumeToken);
        if (timeToLive <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeToLive));
        }

        var expiresAt = invitedAt.Add(timeToLive);
        var session = new IntakeSession
        {
            Id = sessionId,
            OrderId = orderId,
            SubjectId = subjectId,
            CustomerId = customerId,
            Status = IntakeSessionStatus.Invited,
            CreatedAt = invitedAt,
            LastTouchedAt = invitedAt,
            ExpiresAt = expiresAt,
            ResumeToken = resumeToken,
            PolicySnapshot = policySnapshot
        };

        session.AddDomainEvent(new IntakeSessionInvited(
            session.Id,
            session.OrderId,
            session.SubjectId,
            session.CustomerId,
            session.ResumeToken,
            invitedAt,
            expiresAt,
            policySnapshot));

        return session;
    }

    public void Start(DateTimeOffset startedAt, string? deviceInfo)
    {
        EnsureActive();
        if (Status != IntakeSessionStatus.Invited)
        {
            throw new InvalidOperationException("Only invited sessions can be started.");
        }

        EnsureNotExpired(startedAt);

        Status = IntakeSessionStatus.InProgress;
        LastTouchedAt = startedAt;

        AddDomainEvent(new IntakeSessionStarted(Id, OrderId, startedAt, deviceInfo));
    }

    public void SaveProgress(IntakeAnswersSnapshot answers)
    {
        ArgumentNullException.ThrowIfNull(answers);
        EnsureActive();
        if (Status != IntakeSessionStatus.InProgress)
        {
            throw new InvalidOperationException("Progress can only be saved while in progress.");
        }

        AnswersSnapshot = answers;
        LastTouchedAt = answers.UpdatedAt;

        AddDomainEvent(new IntakeProgressSaved(Id, OrderId, answers));
    }

    public void CaptureConsent(ConsentArtifactPointer artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        EnsureActive();
        if (Status is not (IntakeSessionStatus.InProgress or IntakeSessionStatus.AwaitingReview))
        {
            throw new InvalidOperationException("Consent can only be captured for active sessions.");
        }

        ConsentArtifact = artifact;
        LastTouchedAt = artifact.CapturedAt;

        AddDomainEvent(new ConsentCaptured(Id, OrderId, artifact));
    }

    public void Submit(DateTimeOffset submittedAt)
    {
        EnsureActive();
        if (Status != IntakeSessionStatus.InProgress)
        {
            throw new InvalidOperationException("Only in-progress sessions can be submitted.");
        }

        EnsureNotExpired(submittedAt);

        if (ConsentArtifact is null)
        {
            throw new InvalidOperationException("Consent must be captured before submission.");
        }

        if (AnswersSnapshot is null)
        {
            throw new InvalidOperationException("Answers must be saved before submission.");
        }

        Status = IntakeSessionStatus.AwaitingReview;
        SubmittedAt = submittedAt;
        LastTouchedAt = submittedAt;

        AddDomainEvent(new IntakeSubmissionReceived(Id, OrderId, submittedAt));
    }

    public void AcceptSubmission(DateTimeOffset acceptedAt)
    {
        EnsureActive();
        if (Status != IntakeSessionStatus.AwaitingReview)
        {
            throw new InvalidOperationException("Submission can only be accepted from the awaiting review state.");
        }

        Status = IntakeSessionStatus.Submitted;
        AcceptedAt = acceptedAt;
        LastTouchedAt = acceptedAt;

        AddDomainEvent(new IntakeSubmissionAccepted(Id, OrderId, acceptedAt));
    }

    public void Expire(DateTimeOffset expiredAt, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status == IntakeSessionStatus.Submitted || Status == IntakeSessionStatus.Abandoned)
        {
            return;
        }

        Status = IntakeSessionStatus.Abandoned;
        CancellationReason = reason;
        LastTouchedAt = expiredAt;

        AddDomainEvent(new IntakeSessionExpired(Id, OrderId, expiredAt, reason));
    }

    public void Supersede(UlidId supersededBySessionId, DateTimeOffset supersededAt)
    {
        if (Status == IntakeSessionStatus.Submitted)
        {
            throw new InvalidOperationException("Submitted sessions cannot be superseded.");
        }

        SupersededBySessionId = supersededBySessionId;
        Status = IntakeSessionStatus.Abandoned;
        LastTouchedAt = supersededAt;

        AddDomainEvent(new IntakeSessionSuperseded(Id, OrderId, supersededBySessionId, supersededAt));
    }

    private void EnsureNotExpired(DateTimeOffset observedAt)
    {
        if (observedAt > ExpiresAt)
        {
            throw new InvalidOperationException("Intake session has expired.");
        }
    }

    private void EnsureActive()
    {
        if (Status == IntakeSessionStatus.Abandoned)
        {
            throw new InvalidOperationException("Intake session is no longer active.");
        }
    }

    public override string GetStreamId() => $"{GetStreamType()}:{Id}";

    public override string GetStreamType() => "IntakeSession";
}