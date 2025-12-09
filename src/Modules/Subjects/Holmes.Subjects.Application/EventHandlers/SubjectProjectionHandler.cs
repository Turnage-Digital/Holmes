using Holmes.Subjects.Application.Abstractions.Projections;
using Holmes.Subjects.Domain.Events;
using MediatR;

namespace Holmes.Subjects.Application.EventHandlers;

/// <summary>
/// Handles subject domain events to maintain the subject projection table.
/// This replaces the synchronous UpsertDirectory calls in the repository.
/// </summary>
public sealed class SubjectProjectionHandler(ISubjectProjectionWriter writer)
    : INotificationHandler<SubjectRegistered>,
      INotificationHandler<SubjectMerged>,
      INotificationHandler<SubjectAliasAdded>
{
    public Task Handle(SubjectRegistered notification, CancellationToken cancellationToken)
    {
        var model = new SubjectProjectionModel(
            notification.SubjectId.ToString(),
            notification.GivenName,
            notification.FamilyName,
            notification.DateOfBirth,
            notification.Email,
            notification.RegisteredAt,
            IsMerged: false,
            AliasCount: 0);

        return writer.UpsertAsync(model, cancellationToken);
    }

    public Task Handle(SubjectMerged notification, CancellationToken cancellationToken)
    {
        // Mark the source subject as merged
        return writer.UpdateIsMergedAsync(
            notification.SourceSubjectId.ToString(),
            isMerged: true,
            cancellationToken);
    }

    public Task Handle(SubjectAliasAdded notification, CancellationToken cancellationToken)
    {
        return writer.IncrementAliasCountAsync(
            notification.SubjectId.ToString(),
            cancellationToken);
    }
}
