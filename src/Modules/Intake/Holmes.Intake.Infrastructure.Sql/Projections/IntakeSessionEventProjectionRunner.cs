using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Projections;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Intake.Infrastructure.Sql.Projections;

/// <summary>
///     Event-based projection runner for Intake Session projections.
///     Replays IntakeSession domain events to rebuild the intake_session_projections table.
/// </summary>
public sealed class IntakeSessionEventProjectionRunner : EventProjectionRunner
{
    private readonly IntakeDbContext _intakeDbContext;

    public IntakeSessionEventProjectionRunner(
        IntakeDbContext intakeDbContext,
        CoreDbContext coreDbContext,
        IEventStore eventStore,
        IDomainEventSerializer serializer,
        IPublisher publisher,
        ILogger<IntakeSessionEventProjectionRunner> logger
    )
        : base(coreDbContext, eventStore, serializer, publisher, logger)
    {
        _intakeDbContext = intakeDbContext;
    }

    protected override string ProjectionName => "intake.sessions.events";

    protected override string[]? StreamTypes => ["IntakeSession"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (_intakeDbContext.Database.IsRelational())
        {
            await _intakeDbContext.IntakeSessionProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            _intakeDbContext.IntakeSessionProjections.RemoveRange(_intakeDbContext.IntakeSessionProjections);
            await _intakeDbContext.SaveChangesAsync(cancellationToken);
        }

        _intakeDbContext.ChangeTracker.Clear();
    }
}