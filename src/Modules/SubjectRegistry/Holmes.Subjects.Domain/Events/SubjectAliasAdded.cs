using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Subjects.Domain.Events;

public sealed record SubjectAliasAdded(
    UlidId SubjectId,
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    UlidId AddedBy,
    DateTimeOffset AddedAt
) : INotification;