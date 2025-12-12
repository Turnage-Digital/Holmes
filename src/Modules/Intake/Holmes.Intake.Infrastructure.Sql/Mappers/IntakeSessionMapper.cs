using System.Text.Json;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.ValueObjects;
using Holmes.Intake.Infrastructure.Sql.Entities;

namespace Holmes.Intake.Infrastructure.Sql.Mappers;

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

        ConsentArtifactPointer? consent = null;
        if (db.ConsentArtifactId is not null &&
            db.ConsentMimeType is not null &&
            db.ConsentLength is not null &&
            db.ConsentHash is not null &&
            db.ConsentHashAlgorithm is not null &&
            db.ConsentSchemaVersion is not null &&
            db.ConsentCapturedAt is not null)
        {
            consent = ConsentArtifactPointer.Create(
                UlidId.Parse(db.ConsentArtifactId),
                db.ConsentMimeType,
                db.ConsentLength.Value,
                db.ConsentHash,
                db.ConsentHashAlgorithm,
                db.ConsentSchemaVersion,
                db.ConsentCapturedAt.Value);
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
            consent,
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

        if (session.ConsentArtifact is not null)
        {
            db.ConsentArtifactId = session.ConsentArtifact.ArtifactId.ToString();
            db.ConsentMimeType = session.ConsentArtifact.MimeType;
            db.ConsentLength = session.ConsentArtifact.Length;
            db.ConsentHash = session.ConsentArtifact.Hash;
            db.ConsentHashAlgorithm = session.ConsentArtifact.HashAlgorithm;
            db.ConsentSchemaVersion = session.ConsentArtifact.SchemaVersion;
            db.ConsentCapturedAt = session.ConsentArtifact.CapturedAt;
        }
        else
        {
            db.ConsentArtifactId = null;
            db.ConsentMimeType = null;
            db.ConsentLength = null;
            db.ConsentHash = null;
            db.ConsentHashAlgorithm = null;
            db.ConsentSchemaVersion = null;
            db.ConsentCapturedAt = null;
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