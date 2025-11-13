using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Subjects.Domain.Events;

public sealed record SubjectRegistered(
    UlidId SubjectId,
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    string? Email,
    DateTimeOffset RegisteredAt
) : INotification;