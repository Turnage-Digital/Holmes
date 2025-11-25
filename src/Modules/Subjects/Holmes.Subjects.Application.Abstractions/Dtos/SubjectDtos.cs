namespace Holmes.Subjects.Application.Abstractions.Dtos;

public sealed record SubjectSummaryDto(
    string SubjectId,
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    string? Email,
    bool IsMerged,
    int AliasCount,
    DateTimeOffset CreatedAt
);

public sealed record SubjectAliasDto(
    string Id,
    string FirstName,
    string LastName,
    DateOnly? BirthDate,
    DateTimeOffset CreatedAt
);

public sealed record SubjectListItemDto(
    string Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly? BirthDate,
    string? Email,
    string Status,
    string? MergeParentId,
    IReadOnlyCollection<SubjectAliasDto> Aliases,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);