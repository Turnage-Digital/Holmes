using Holmes.Core.Application;

namespace Holmes.Subjects.Application.Abstractions.Queries;

public sealed record CheckSubjectExistsQuery(
    string SubjectId
) : RequestBase<bool>;
