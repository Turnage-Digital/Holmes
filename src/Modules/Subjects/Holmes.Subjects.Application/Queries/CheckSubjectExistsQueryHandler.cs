using Holmes.Subjects.Application.Abstractions;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

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