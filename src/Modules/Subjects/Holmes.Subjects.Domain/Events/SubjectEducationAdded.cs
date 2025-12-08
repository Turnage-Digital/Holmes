using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Subjects.Domain.Events;

public sealed record SubjectEducationAdded(
    UlidId SubjectId,
    UlidId EducationId,
    string InstitutionName,
    string? Degree,
    DateTimeOffset Timestamp) : INotification;
