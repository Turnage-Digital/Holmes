using Holmes.Core.Application;

namespace Holmes.Subjects.Application.Queries;

public sealed record CheckSubjectExistsQuery(
    string SubjectId
) : RequestBase<bool>;