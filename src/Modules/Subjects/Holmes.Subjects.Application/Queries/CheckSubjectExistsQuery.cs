using Holmes.Core.Contracts;

namespace Holmes.Subjects.Application.Queries;

public sealed record CheckSubjectExistsQuery(
    string SubjectId
) : RequestBase<bool>;