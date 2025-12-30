using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Subjects.Contracts.Dtos;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectPhonesQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectPhoneDto>>>;