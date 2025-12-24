using Holmes.Subjects.Application.Abstractions.Dtos;

namespace Holmes.Subjects.Application.Abstractions;

/// <summary>
///     Query interface for subject lookups. Used by application layer for read operations.
/// </summary>
public interface ISubjectQueries
{
    /// <summary>
    ///     Gets a paginated list of subjects.
    /// </summary>
    Task<SubjectPagedResult> GetSubjectsPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets a subject summary by ID (from directory).
    /// </summary>
    Task<SubjectSummaryDto?> GetSummaryByIdAsync(string subjectId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets a subject with full details including all history collections.
    /// </summary>
    Task<SubjectDetailDto?> GetDetailByIdAsync(string subjectId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets addresses for a subject.
    /// </summary>
    Task<IReadOnlyList<SubjectAddressDto>> GetAddressesAsync(string subjectId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets employments for a subject.
    /// </summary>
    Task<IReadOnlyList<SubjectEmploymentDto>> GetEmploymentsAsync(
        string subjectId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets educations for a subject.
    /// </summary>
    Task<IReadOnlyList<SubjectEducationDto>> GetEducationsAsync(string subjectId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets references for a subject.
    /// </summary>
    Task<IReadOnlyList<SubjectReferenceDto>> GetReferencesAsync(string subjectId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets phone numbers for a subject.
    /// </summary>
    Task<IReadOnlyList<SubjectPhoneDto>> GetPhonesAsync(string subjectId, CancellationToken cancellationToken);

    /// <summary>
    ///     Checks if a subject exists.
    /// </summary>
    Task<bool> ExistsAsync(string subjectId, CancellationToken cancellationToken);
}

/// <summary>
///     Paginated result for subject queries.
/// </summary>
public sealed record SubjectPagedResult(
    IReadOnlyList<SubjectListItemDto> Items,
    int TotalCount
);