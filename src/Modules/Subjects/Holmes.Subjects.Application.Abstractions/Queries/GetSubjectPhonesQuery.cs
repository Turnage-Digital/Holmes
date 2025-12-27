using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions.Dtos;

namespace Holmes.Subjects.Application.Abstractions.Queries;

public sealed record GetSubjectPhonesQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectPhoneDto>>>;