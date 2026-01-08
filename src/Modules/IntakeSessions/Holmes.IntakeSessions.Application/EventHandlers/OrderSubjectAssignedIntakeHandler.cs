using System.Security.Cryptography;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.ValueObjects;
using Holmes.Orders.Contracts;
using Holmes.Orders.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.IntakeSessions.Application.EventHandlers;

public sealed class OrderSubjectAssignedIntakeHandler(
    IIntakeSessionsUnitOfWork unitOfWork,
    IOrderQueries orderQueries,
    ILogger<OrderSubjectAssignedIntakeHandler> logger
) : INotificationHandler<OrderSubjectAssignedIntegrationEvent>
{
    private static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromHours(168);

    public async Task Handle(OrderSubjectAssignedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.IntakeSessions.GetByOrderIdAsync(
            notification.OrderId,
            cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var summary = await orderQueries.GetSummaryByIdAsync(notification.OrderId.ToString(), cancellationToken);
        if (summary is null)
        {
            logger.LogWarning(
                "Order summary missing for Order {OrderId}, intake session not created",
                notification.OrderId);
            return;
        }

        var session = IntakeSession.Invite(
            UlidId.NewUlid(),
            notification.OrderId,
            notification.SubjectId,
            notification.CustomerId,
            PolicySnapshot.Create(
                summary.PolicySnapshotId,
                "v1",
                notification.OccurredAt,
                new Dictionary<string, string>()),
            GenerateResumeToken(),
            notification.OccurredAt,
            DefaultTimeToLive);

        await unitOfWork.IntakeSessions.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }

    private static string GenerateResumeToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}
