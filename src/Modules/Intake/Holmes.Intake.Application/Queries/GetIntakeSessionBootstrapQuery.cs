using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain;
using MediatR;

namespace Holmes.Intake.Application.Queries;

public sealed record GetIntakeSessionBootstrapQuery(
    UlidId IntakeSessionId,
    string ResumeToken
) : RequestBase<IntakeSessionBootstrap?>;

public sealed record IntakeSessionBootstrap(
    string SessionId,
    string OrderId,
    string SubjectId,
    string CustomerId,
    string PolicySnapshotId,
    string PolicySnapshotSchemaVersion,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastTouchedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? AcceptedAt,
    string? CancellationReason,
    string? SupersededBySessionId,
    BootstrapConsentArtifact? Consent,
    BootstrapAnswersSnapshot? Answers
);

public sealed record BootstrapConsentArtifact(
    string ArtifactId,
    string MimeType,
    long Length,
    string Hash,
    string HashAlgorithm,
    string SchemaVersion,
    DateTimeOffset CapturedAt
);

public sealed record BootstrapAnswersSnapshot(
    string SchemaVersion,
    string PayloadHash,
    string PayloadCipherText,
    DateTimeOffset UpdatedAt
);

public sealed class GetIntakeSessionBootstrapQueryHandler(IIntakeUnitOfWork unitOfWork)
    : IRequestHandler<GetIntakeSessionBootstrapQuery, IntakeSessionBootstrap?>
{
    public async Task<IntakeSessionBootstrap?> Handle(
        GetIntakeSessionBootstrapQuery request,
        CancellationToken cancellationToken
    )
    {
        var session = await unitOfWork.IntakeSessions.GetByIdAsync(request.IntakeSessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (!string.Equals(session.ResumeToken, request.ResumeToken, StringComparison.Ordinal))
        {
            return null;
        }

        BootstrapConsentArtifact? consent = null;
        if (session.ConsentArtifact is not null)
        {
            consent = new BootstrapConsentArtifact(
                session.ConsentArtifact.ArtifactId.ToString(),
                session.ConsentArtifact.MimeType,
                session.ConsentArtifact.Length,
                session.ConsentArtifact.Hash,
                session.ConsentArtifact.HashAlgorithm,
                session.ConsentArtifact.SchemaVersion,
                session.ConsentArtifact.CapturedAt);
        }

        BootstrapAnswersSnapshot? answers = null;
        if (session.AnswersSnapshot is not null)
        {
            answers = new BootstrapAnswersSnapshot(
                session.AnswersSnapshot.SchemaVersion,
                session.AnswersSnapshot.PayloadHash,
                session.AnswersSnapshot.PayloadCipherText,
                session.AnswersSnapshot.UpdatedAt);
        }

        return new IntakeSessionBootstrap(
            session.Id.ToString(),
            session.OrderId.ToString(),
            session.SubjectId.ToString(),
            session.CustomerId.ToString(),
            session.PolicySnapshot.SnapshotId,
            session.PolicySnapshot.SchemaVersion,
            session.Status.ToString(),
            session.ExpiresAt,
            session.CreatedAt,
            session.LastTouchedAt,
            session.SubmittedAt,
            session.AcceptedAt,
            session.CancellationReason,
            session.SupersededBySessionId?.ToString(),
            consent,
            answers);
    }
}
