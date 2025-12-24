using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Projections;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Subjects.Infrastructure.Sql;

/// <summary>
///     Event-based projection runner for Subject projections.
///     Replays Subject domain events to rebuild the subject_projections table.
/// </summary>
public sealed class SubjectEventProjectionRunner : EventProjectionRunner
{
    private readonly SubjectsDbContext _subjectsDbContext;

    public SubjectEventProjectionRunner(
        SubjectsDbContext subjectsDbContext,
        CoreDbContext coreDbContext,
        IEventStore eventStore,
        IDomainEventSerializer serializer,
        IPublisher publisher,
        ILogger<SubjectEventProjectionRunner> logger
    )
        : base(coreDbContext, eventStore, serializer, publisher, logger)
    {
        _subjectsDbContext = subjectsDbContext;
    }

    protected override string ProjectionName => "subjects.subject_projection.events";

    protected override string[]? StreamTypes => ["Subject"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (_subjectsDbContext.Database.IsRelational())
        {
            await _subjectsDbContext.SubjectProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            _subjectsDbContext.SubjectProjections.RemoveRange(_subjectsDbContext.SubjectProjections);
            await _subjectsDbContext.SaveChangesAsync(cancellationToken);
        }

        _subjectsDbContext.ChangeTracker.Clear();
    }
}