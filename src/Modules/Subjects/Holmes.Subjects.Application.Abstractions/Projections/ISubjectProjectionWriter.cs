namespace Holmes.Subjects.Application.Abstractions.Projections;

public interface ISubjectProjectionWriter
{
    Task UpsertAsync(SubjectProjectionModel model, CancellationToken cancellationToken);
    Task UpdateIsMergedAsync(string subjectId, bool isMerged, CancellationToken cancellationToken);
    Task IncrementAliasCountAsync(string subjectId, CancellationToken cancellationToken);
}

public sealed record SubjectProjectionModel(
    string SubjectId,
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    string? Email,
    DateTimeOffset CreatedAt,
    bool IsMerged,
    int AliasCount);
