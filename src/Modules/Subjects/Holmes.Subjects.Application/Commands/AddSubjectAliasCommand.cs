using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Application.Commands;

public sealed record AddSubjectAliasCommand(
    UlidId TargetSubjectId,
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    DateTimeOffset AddedAt
) : RequestBase<Result>;