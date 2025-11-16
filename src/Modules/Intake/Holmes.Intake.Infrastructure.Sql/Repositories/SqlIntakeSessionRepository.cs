using System.Text.Json;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.ValueObjects;
using Holmes.Intake.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Intake.Infrastructure.Sql.Repositories;

public class SqlIntakeSessionRepository(IntakeDbContext dbContext)
    : IIntakeSessionRepository
{
    public Task AddAsync(IntakeSession session, CancellationToken cancellationToken)
    {
        var entity = ToDb(session);
        dbContext.IntakeSessions.Add(entity);
        return Task.CompletedTask;
    }

    public async Task<IntakeSession?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
    {
        var record = await dbContext.IntakeSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IntakeSessionId == id.ToString(), cancellationToken);

        return record is null ? null : Rehydrate(record);
    }

    public async Task UpdateAsync(IntakeSession session, CancellationToken cancellationToken)
    {
        var record = await dbContext.IntakeSessions
            .FirstOrDefaultAsync(x => x.IntakeSessionId == session.Id.ToString(), cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException($"Intake session '{session.Id}' not found.");
        }

        ApplyState(session, record);
    }

    private static IntakeSession Rehydrate(IntakeSessionDb record)
    {
        var policy = JsonSerializer.Deserialize<PolicySnapshotRecord>(record.PolicySnapshotJson)
                     ?? throw new InvalidOperationException("Policy snapshot payload invalid.");

        var policySnapshot = PolicySnapshot.Create(
            policy.SnapshotId,
            policy.SchemaVersion,
            policy.CapturedAt,
            policy.Metadata);

        IntakeAnswersSnapshot? answers = null;
        if (record.AnswersSchemaVersion is not null && record.AnswersPayloadHash is not null &&
            record.AnswersPayloadCipherText is not null && record.AnswersUpdatedAt is not null)
        {
            answers = IntakeAnswersSnapshot.Create(
                record.AnswersSchemaVersion,
                record.AnswersPayloadHash,
                record.AnswersPayloadCipherText,
                record.AnswersUpdatedAt.Value);
        }

        ConsentArtifactPointer? consent = null;
        if (record.ConsentArtifactId is not null &&
            record.ConsentMimeType is not null &&
            record.ConsentLength is not null &&
            record.ConsentHash is not null &&
            record.ConsentHashAlgorithm is not null &&
            record.ConsentSchemaVersion is not null &&
            record.ConsentCapturedAt is not null)
        {
            consent = ConsentArtifactPointer.Create(
                UlidId.Parse(record.ConsentArtifactId),
                record.ConsentMimeType,
                record.ConsentLength.Value,
                record.ConsentHash,
                record.ConsentHashAlgorithm,
                record.ConsentSchemaVersion,
                record.ConsentCapturedAt.Value);
        }

        return IntakeSession.Rehydrate(
            UlidId.Parse(record.IntakeSessionId),
            UlidId.Parse(record.OrderId),
            UlidId.Parse(record.SubjectId),
            UlidId.Parse(record.CustomerId),
            Enum.Parse<IntakeSessionStatus>(record.Status),
            record.CreatedAt,
            record.LastTouchedAt,
            record.ExpiresAt,
            record.ResumeToken,
            policySnapshot,
            answers,
            consent,
            record.SubmittedAt,
            record.AcceptedAt,
            record.CancellationReason,
            record.SupersededBySessionId is null ? null : UlidId.Parse(record.SupersededBySessionId));
    }

    private static IntakeSessionDb ToDb(IntakeSession session)
    {
        var entity = new IntakeSessionDb();
        ApplyState(session, entity); // populates fields once Id set
        return entity;
    }

    private static void ApplyState(IntakeSession session, IntakeSessionDb record)
    {
        record.IntakeSessionId = session.Id.ToString();
        record.OrderId = session.OrderId.ToString();
        record.SubjectId = session.SubjectId.ToString();
        record.CustomerId = session.CustomerId.ToString();
        record.Status = session.Status.ToString();
        record.CreatedAt = session.CreatedAt;
        record.LastTouchedAt = session.LastTouchedAt;
        record.ExpiresAt = session.ExpiresAt;
        record.ResumeToken = session.ResumeToken;
        record.PolicySnapshotJson = JsonSerializer.Serialize(new PolicySnapshotRecord(
            session.PolicySnapshot.SnapshotId,
            session.PolicySnapshot.SchemaVersion,
            session.PolicySnapshot.CapturedAt,
            session.PolicySnapshot.Metadata));

        if (session.AnswersSnapshot is not null)
        {
            record.AnswersSchemaVersion = session.AnswersSnapshot.SchemaVersion;
            record.AnswersPayloadHash = session.AnswersSnapshot.PayloadHash;
            record.AnswersPayloadCipherText = session.AnswersSnapshot.PayloadCipherText;
            record.AnswersUpdatedAt = session.AnswersSnapshot.UpdatedAt;
        }
        else
        {
            record.AnswersSchemaVersion = null;
            record.AnswersPayloadHash = null;
            record.AnswersPayloadCipherText = null;
            record.AnswersUpdatedAt = null;
        }

        if (session.ConsentArtifact is not null)
        {
            record.ConsentArtifactId = session.ConsentArtifact.ArtifactId.ToString();
            record.ConsentMimeType = session.ConsentArtifact.MimeType;
            record.ConsentLength = session.ConsentArtifact.Length;
            record.ConsentHash = session.ConsentArtifact.Hash;
            record.ConsentHashAlgorithm = session.ConsentArtifact.HashAlgorithm;
            record.ConsentSchemaVersion = session.ConsentArtifact.SchemaVersion;
            record.ConsentCapturedAt = session.ConsentArtifact.CapturedAt;
        }
        else
        {
            record.ConsentArtifactId = null;
            record.ConsentMimeType = null;
            record.ConsentLength = null;
            record.ConsentHash = null;
            record.ConsentHashAlgorithm = null;
            record.ConsentSchemaVersion = null;
            record.ConsentCapturedAt = null;
        }

        record.SubmittedAt = session.SubmittedAt;
        record.AcceptedAt = session.AcceptedAt;
        record.CancellationReason = session.CancellationReason;
        record.SupersededBySessionId = session.SupersededBySessionId?.ToString();
    }

    private sealed record PolicySnapshotRecord(
        string SnapshotId,
        string SchemaVersion,
        DateTimeOffset CapturedAt,
        IReadOnlyDictionary<string, string> Metadata
    );
}