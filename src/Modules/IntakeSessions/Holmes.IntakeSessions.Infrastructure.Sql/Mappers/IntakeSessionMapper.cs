using System.Text.Json;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.ValueObjects;
using Holmes.IntakeSessions.Infrastructure.Sql.Entities;

namespace Holmes.IntakeSessions.Infrastructure.Sql.Mappers;

public static class IntakeSessionMapper
{
    public static IntakeSession ToDomain(IntakeSessionDb db)
    {
        var policy = JsonSerializer.Deserialize<PolicySnapshotRecord>(db.PolicySnapshotJson)
                     ?? throw new InvalidOperationException("Policy snapshot payload invalid.");

        var policySnapshot = PolicySnapshot.Create(
            policy.SnapshotId,
            policy.SchemaVersion,
            policy.CapturedAt,
            policy.Metadata);

        IntakeAnswersSnapshot? answers = null;
        if (db.AnswersSchemaVersion is not null && db.AnswersPayloadHash is not null &&
            db.AnswersPayloadCipherText is not null && db.AnswersUpdatedAt is not null)
        {
            answers = IntakeAnswersSnapshot.Create(
                db.AnswersSchemaVersion,
                db.AnswersPayloadHash,
                db.AnswersPayloadCipherText,
                db.AnswersUpdatedAt.Value);
        }

        AuthorizationArtifactPointer? authorization = null;
        if (db.AuthorizationArtifactId is not null &&
            db.AuthorizationMimeType is not null &&
            db.AuthorizationLength is not null &&
            db.AuthorizationHash is not null &&
            db.AuthorizationHashAlgorithm is not null &&
            db.AuthorizationSchemaVersion is not null &&
            db.AuthorizationCapturedAt is not null)
        {
            authorization = AuthorizationArtifactPointer.Create(
                UlidId.Parse(db.AuthorizationArtifactId),
                db.AuthorizationMimeType,
                db.AuthorizationLength.Value,
                db.AuthorizationHash,
                db.AuthorizationHashAlgorithm,
                db.AuthorizationSchemaVersion,
                db.AuthorizationCapturedAt.Value);
        }

        return IntakeSession.Rehydrate(
            UlidId.Parse(db.IntakeSessionId),
            UlidId.Parse(db.OrderId),
            UlidId.Parse(db.SubjectId),
            UlidId.Parse(db.CustomerId),
            Enum.Parse<IntakeSessionStatus>(db.Status),
            db.CreatedAt,
            db.LastTouchedAt,
            db.ExpiresAt,
            db.ResumeToken,
            policySnapshot,
            answers,
            authorization,
            db.SubmittedAt,
            db.AcceptedAt,
            db.CancellationReason,
            db.SupersededBySessionId is null ? null : UlidId.Parse(db.SupersededBySessionId));
    }

    public static IntakeSessionDb ToDb(IntakeSession session)
    {
        var db = new IntakeSessionDb();
        UpdateDb(db, session);
        return db;
    }

    public static void UpdateDb(IntakeSessionDb db, IntakeSession session)
    {
        db.IntakeSessionId = session.Id.ToString();
        db.OrderId = session.OrderId.ToString();
        db.SubjectId = session.SubjectId.ToString();
        db.CustomerId = session.CustomerId.ToString();
        db.Status = session.Status.ToString();
        db.CreatedAt = session.CreatedAt;
        db.LastTouchedAt = session.LastTouchedAt;
        db.ExpiresAt = session.ExpiresAt;
        db.ResumeToken = session.ResumeToken;
        db.PolicySnapshotJson = JsonSerializer.Serialize(new PolicySnapshotRecord(
            session.PolicySnapshot.SnapshotId,
            session.PolicySnapshot.SchemaVersion,
            session.PolicySnapshot.CapturedAt,
            session.PolicySnapshot.Metadata));

        if (session.AnswersSnapshot is not null)
        {
            db.AnswersSchemaVersion = session.AnswersSnapshot.SchemaVersion;
            db.AnswersPayloadHash = session.AnswersSnapshot.PayloadHash;
            db.AnswersPayloadCipherText = session.AnswersSnapshot.PayloadCipherText;
            db.AnswersUpdatedAt = session.AnswersSnapshot.UpdatedAt;
        }
        else
        {
            db.AnswersSchemaVersion = null;
            db.AnswersPayloadHash = null;
            db.AnswersPayloadCipherText = null;
            db.AnswersUpdatedAt = null;
        }

        if (session.AuthorizationArtifact is not null)
        {
            db.AuthorizationArtifactId = session.AuthorizationArtifact.ArtifactId.ToString();
            db.AuthorizationMimeType = session.AuthorizationArtifact.MimeType;
            db.AuthorizationLength = session.AuthorizationArtifact.Length;
            db.AuthorizationHash = session.AuthorizationArtifact.Hash;
            db.AuthorizationHashAlgorithm = session.AuthorizationArtifact.HashAlgorithm;
            db.AuthorizationSchemaVersion = session.AuthorizationArtifact.SchemaVersion;
            db.AuthorizationCapturedAt = session.AuthorizationArtifact.CapturedAt;
        }
        else
        {
            db.AuthorizationArtifactId = null;
            db.AuthorizationMimeType = null;
            db.AuthorizationLength = null;
            db.AuthorizationHash = null;
            db.AuthorizationHashAlgorithm = null;
            db.AuthorizationSchemaVersion = null;
            db.AuthorizationCapturedAt = null;
        }

        db.SubmittedAt = session.SubmittedAt;
        db.AcceptedAt = session.AcceptedAt;
        db.CancellationReason = session.CancellationReason;
        db.SupersededBySessionId = session.SupersededBySessionId?.ToString();
    }

    internal sealed record PolicySnapshotRecord(
        string SnapshotId,
        string SchemaVersion,
        DateTimeOffset CapturedAt,
        IReadOnlyDictionary<string, string> Metadata
    );
}
