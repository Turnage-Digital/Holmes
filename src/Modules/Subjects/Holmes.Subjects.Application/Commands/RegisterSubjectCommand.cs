using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Subjects.Contracts.Dtos;

namespace Holmes.Subjects.Application.Commands;

public sealed record RegisterSubjectCommand(
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    string? Email,
    DateTimeOffset RegisteredAt
) : RequestBase<Result<SubjectSummaryDto>>;