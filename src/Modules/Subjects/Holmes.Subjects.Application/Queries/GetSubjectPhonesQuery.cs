using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions.Dtos;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectPhonesQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectPhoneDto>>>;