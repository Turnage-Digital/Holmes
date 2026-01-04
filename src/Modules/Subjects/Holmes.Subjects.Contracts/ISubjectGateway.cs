using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Contracts;

public interface ISubjectGateway
{
    Task<EnsureSubjectResult> EnsureSubjectAsync(
        string email,
        string? phone,
        DateTimeOffset requestedAt,
        CancellationToken cancellationToken
    );
}

public sealed record EnsureSubjectResult(UlidId SubjectId, bool WasExisting);