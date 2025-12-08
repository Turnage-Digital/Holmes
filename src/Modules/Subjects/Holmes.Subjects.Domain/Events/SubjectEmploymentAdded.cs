using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Subjects.Domain.Events;

public sealed record SubjectEmploymentAdded(
    UlidId SubjectId,
    UlidId EmploymentId,
    string EmployerName,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateTimeOffset Timestamp
) : INotification;