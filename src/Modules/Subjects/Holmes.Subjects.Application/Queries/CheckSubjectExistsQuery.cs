using Holmes.Core.Application;
using Holmes.Subjects.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

public sealed record CheckSubjectExistsQuery(
    string SubjectId
) : RequestBase<bool>;

public sealed class CheckSubjectExistsQueryHandler(
    ISubjectQueries subjectQueries
) : IRequestHandler<CheckSubjectExistsQuery, bool>
{
    public async Task<bool> Handle(
        CheckSubjectExistsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await subjectQueries.ExistsAsync(request.SubjectId, cancellationToken);
    }
}