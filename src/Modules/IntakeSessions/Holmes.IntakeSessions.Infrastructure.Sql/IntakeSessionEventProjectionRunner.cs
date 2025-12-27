using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Projections;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.IntakeSessions.Infrastructure.Sql;

/// <summary>
///     Event-based projection runner for Intake Session projections.
///     Replays IntakeSession domain events to rebuild the intake_session_projections table.
/// </summary>
public sealed class IntakeSessionEventProjectionRunner(
    IntakeSessionsDbContext intakeDbContext,
    CoreDbContext coreDbContext,
    IEventStore eventStore,
    IDomainEventSerializer serializer,
    IPublisher publisher,
    ILogger<IntakeSessionEventProjectionRunner> logger
)
    : EventProjectionRunner(coreDbContext, eventStore, serializer, publisher, logger)
{
    protected override string ProjectionName => "intake.sessions.events";

    protected override string[] StreamTypes => ["IntakeSession"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (intakeDbContext.Database.IsRelational())
        {
            await intakeDbContext.IntakeSessionProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            intakeDbContext.IntakeSessionProjections.RemoveRange(intakeDbContext.IntakeSessionProjections);
            await intakeDbContext.SaveChangesAsync(cancellationToken);
        }

        intakeDbContext.ChangeTracker.Clear();
    }
}