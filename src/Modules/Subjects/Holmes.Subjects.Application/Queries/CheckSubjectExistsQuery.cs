using Holmes.Core.Application.Abstractions;

namespace Holmes.Subjects.Application.Queries;

public sealed record CheckSubjectExistsQuery(
    string SubjectId
) : RequestBase<bool>;