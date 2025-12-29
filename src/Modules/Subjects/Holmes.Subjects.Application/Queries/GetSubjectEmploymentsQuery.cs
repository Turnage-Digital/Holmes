using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions.Dtos;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectEmploymentsQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectEmploymentDto>>>;