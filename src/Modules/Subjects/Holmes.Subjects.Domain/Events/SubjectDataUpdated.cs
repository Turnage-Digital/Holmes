using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Subjects.Domain.Events;

public sealed record SubjectDataUpdated(
    UlidId SubjectId,
    int AddressCount,
    int EmploymentCount,
    int EducationCount,
    int ReferenceCount,
    int PhoneCount,
    bool HasSsn,
    DateTimeOffset Timestamp
) : INotification;