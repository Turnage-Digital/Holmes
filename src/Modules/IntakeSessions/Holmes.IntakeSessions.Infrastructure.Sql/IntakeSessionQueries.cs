using System.Text.Json;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Contracts;
using Holmes.IntakeSessions.Contracts.Dtos;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.IntakeSessions.Infrastructure.Sql;

public sealed class IntakeSessionQueries(IntakeSessionsDbContext dbContext) : IIntakeSessionQueries
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
        // Parse policy snapshot JSON to extract ID, schema version, and metadata
        var policyJson = JsonDocument.Parse(session.PolicySnapshotJson);
        var policySnapshotId = policyJson.RootElement.GetProperty("snapshotId").GetString() ?? "";
        var policySchemaVersion = policyJson.RootElement.GetProperty("schemaVersion").GetString() ?? "";

        // Extract section config from policy metadata
        var sectionConfig = ParseSectionConfig(policyJson.RootElement);

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
            answers,
            sectionConfig
        );
    }

    private static IntakeSectionConfigDto? ParseSectionConfig(JsonElement policyElement)
    {
        // Try to get metadata from the policy snapshot
        if (!policyElement.TryGetProperty("metadata", out var metadataElement) ||
            metadataElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        // Parse requiredSections (comma-separated string)
        var requiredSections = new List<string>();
        if (metadataElement.TryGetProperty("requiredSections", out var sectionsElement) &&
            sectionsElement.ValueKind == JsonValueKind.String)
        {
            var sectionsStr = sectionsElement.GetString();
            if (!string.IsNullOrEmpty(sectionsStr))
            {
                requiredSections.AddRange(sectionsStr.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }
        }

        // Parse orderedServices (comma-separated string)
        var enabledServiceCodes = new List<string>();
        if (metadataElement.TryGetProperty("orderedServices", out var servicesElement) &&
            servicesElement.ValueKind == JsonValueKind.String)
        {
            var servicesStr = servicesElement.GetString();
            if (!string.IsNullOrEmpty(servicesStr))
            {
                enabledServiceCodes.AddRange(servicesStr.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }
        }

        // Only return config if we have any section info
        if (requiredSections.Count == 0 && enabledServiceCodes.Count == 0)
        {
            return null;
        }

        return new IntakeSectionConfigDto(requiredSections, enabledServiceCodes);
    }
}