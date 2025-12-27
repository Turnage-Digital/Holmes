using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Application.Abstractions.Commands;

public sealed record RegisterSubjectCommand(
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    string? Email,
    DateTimeOffset RegisteredAt
) : RequestBase<UlidId>;
