using System.Text.Json;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Abstractions.Dtos;
using Holmes.Intake.Application.Abstractions.Queries;
using Holmes.Intake.Domain;
using Holmes.Intake.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Intake.Infrastructure.Sql.Queries;

public sealed class SqlIntakeSessionQueries(IntakeDbContext dbContext) : IIntakeSessionQueries
{
    public async Task<IntakeSessionBootstrapDto?> GetBootstrapAsync(
        UlidId intakeSessionId,
        string resumeToken,
        CancellationToken cancellationToken
    )
    {
        var sessionIdStr = intakeSessionId.ToString();

        var session = await dbContext.IntakeSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.IntakeSessionId == sessionIdStr, cancellationToken);

        if (session is null)
        {
            return null;
        }

        if (!string.Equals(session.ResumeToken, resumeToken, StringComparison.Ordinal))
        {
            return null;
        }

        return MapToBootstrapDto(session);
    }

    public async Task<IntakeSessionSummaryDto?> GetByIdAsync(
        string intakeSessionId,
        CancellationToken cancellationToken
    )
    {
        var projection = await dbContext.IntakeSessionProjections
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.IntakeSessionId == intakeSessionId, cancellationToken);

        return projection is null ? null : MapToSummaryDto(projection);
    }

    public async Task<IReadOnlyList<IntakeSessionSummaryDto>> GetByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    )
    {
        var projections = await dbContext.IntakeSessionProjections
            .AsNoTracking()
            .Where(s => s.OrderId == orderId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        return projections.Select(MapToSummaryDto).ToList();
    }

    public async Task<IntakeSessionSummaryDto?> GetActiveByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    )
    {
        var activeStatuses = new[]
        {
            IntakeSessionStatus.Invited.ToString(),
            IntakeSessionStatus.InProgress.ToString(),
            IntakeSessionStatus.AwaitingReview.ToString()
        };

        var projection = await dbContext.IntakeSessionProjections
            .AsNoTracking()
            .Where(s => s.OrderId == orderId)
            .Where(s => activeStatuses.Contains(s.Status))
            .Where(s => s.SupersededBySessionId == null)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return projection is null ? null : MapToSummaryDto(projection);
    }

    public async Task<bool> ExistsAsync(string intakeSessionId, CancellationToken cancellationToken)
    {
        return await dbContext.IntakeSessionProjections
            .AsNoTracking()
            .AnyAsync(s => s.IntakeSessionId == intakeSessionId, cancellationToken);
    }

    private static IntakeSessionSummaryDto MapToSummaryDto(IntakeSessionProjectionDb projection)
    {
        return new IntakeSessionSummaryDto(
            projection.IntakeSessionId,
            projection.OrderId,
            projection.SubjectId,
            projection.CustomerId,
            projection.PolicySnapshotId,
            projection.PolicySnapshotSchemaVersion,
            Enum.Parse<IntakeSessionStatus>(projection.Status),
            projection.ExpiresAt,
            projection.CreatedAt,
            projection.LastTouchedAt,
            projection.SubmittedAt,
            projection.AcceptedAt,
            projection.CancellationReason,
            projection.SupersededBySessionId
        );
    }

    private static IntakeSessionBootstrapDto MapToBootstrapDto(IntakeSessionDb session)
    {
        // Parse policy snapshot JSON to extract ID and schema version
        var policyJson = JsonDocument.Parse(session.PolicySnapshotJson);
        var policySnapshotId = policyJson.RootElement.GetProperty("snapshotId").GetString() ?? "";
        var policySchemaVersion = policyJson.RootElement.GetProperty("schemaVersion").GetString() ?? "";

        ConsentArtifactDto? consent = null;
        if (session.ConsentArtifactId is not null &&
            session.ConsentMimeType is not null &&
            session.ConsentLength.HasValue &&
            session.ConsentHash is not null &&
            session.ConsentHashAlgorithm is not null &&
            session.ConsentSchemaVersion is not null &&
            session.ConsentCapturedAt.HasValue)
        {
            consent = new ConsentArtifactDto(
                session.ConsentArtifactId,
                session.ConsentMimeType,
                session.ConsentLength.Value,
                session.ConsentHash,
                session.ConsentHashAlgorithm,
                session.ConsentSchemaVersion,
                session.ConsentCapturedAt.Value
            );
        }

        AnswersSnapshotDto? answers = null;
        if (session.AnswersSchemaVersion is not null &&
            session.AnswersPayloadHash is not null &&
            session.AnswersPayloadCipherText is not null &&
            session.AnswersUpdatedAt.HasValue)
        {
            answers = new AnswersSnapshotDto(
                session.AnswersSchemaVersion,
                session.AnswersPayloadHash,
                session.AnswersPayloadCipherText,
                session.AnswersUpdatedAt.Value
            );
        }

        return new IntakeSessionBootstrapDto(
            session.IntakeSessionId,
            session.OrderId,
            session.SubjectId,
            session.CustomerId,
            policySnapshotId,
            policySchemaVersion,
            session.Status,
            session.ExpiresAt,
            session.CreatedAt,
            session.LastTouchedAt,
            session.SubmittedAt,
            session.AcceptedAt,
            session.CancellationReason,
            session.SupersededBySessionId,
            consent,
            answers
        );
    }
}