using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Application.Commands;

public sealed record UpdateSubjectProfileCommand(
    UlidId TargetSubjectId,
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    string? Email
) : RequestBase<Result>;