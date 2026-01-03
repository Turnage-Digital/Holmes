using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Subjects.Contracts.Dtos;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectPhonesQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectPhoneDto>>>;