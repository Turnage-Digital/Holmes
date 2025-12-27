using System.Security.Cryptography;
using System.Transactions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.ValueObjects;
using Holmes.Orders.Domain;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.App.Application.Commands;

/// <summary>
///     Creates an order with intake session in a single atomic operation.
///     Orchestrates across Subjects, Orders, and Intake modules using
///     the outbox pattern for reliable event dispatch.
/// </summary>
public sealed record CreateOrderWithIntakeCommand(
    string SubjectEmail,
    string? SubjectPhone,
    UlidId CustomerId,
    string PolicySnapshotId
) : IRequest<Result<CreateOrderWithIntakeResult>>;

public sealed record CreateOrderWithIntakeResult(
    UlidId SubjectId,
    bool SubjectWasExisting,
    UlidId OrderId,
    UlidId IntakeSessionId,
    string ResumeToken,
    DateTimeOffset ExpiresAt
);

public sealed class CreateOrderWithIntakeCommandHandler(
    ISubjectsUnitOfWork subjectsUnitOfWork,
    IOrdersUnitOfWork ordersUnitOfWork,
    IIntakeSessionsUnitOfWork intakeSessionsUnitOfWork
) : IRequestHandler<CreateOrderWithIntakeCommand, Result<CreateOrderWithIntakeResult>>
{
    private const int DefaultTimeToLiveHours = 168; // 7 days

    public async Task<Result<CreateOrderWithIntakeResult>> Handle(
        CreateOrderWithIntakeCommand request,
        CancellationToken cancellationToken
    )
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.SubjectEmail))
        {
            return Result.Fail<CreateOrderWithIntakeResult>("Subject email is required.");
        }

        // Use TransactionScope for cross-module atomicity
        // All three DbContexts share the same connection string, so they'll enlist
        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        try
        {
            var now = DateTimeOffset.UtcNow;
            var subjectWasExisting = false;

            // 1. Find or create Subject
            var subject = await subjectsUnitOfWork.Subjects
                .GetByEmailAsync(request.SubjectEmail, cancellationToken);

            if (subject is not null)
            {
                subjectWasExisting = true;

                // Add phone if provided (for OTP verification)
                if (!string.IsNullOrWhiteSpace(request.SubjectPhone) &&
                    !subject.Phones.Any(p => p.PhoneNumber == request.SubjectPhone))
                {
                    var phone = SubjectPhone.Create(
                        UlidId.NewUlid(),
                        request.SubjectPhone,
                        PhoneType.Mobile,
                        subject.Phones.Count == 0,
                        now);
                    subject.AddPhone(phone);
                    await subjectsUnitOfWork.Subjects.UpdateAsync(subject, cancellationToken);
                }
            }
            else
            {
                // Create new subject with minimal info - intake will collect the rest
                subject = Subject.Register(
                    UlidId.NewUlid(),
                    "",
                    "",
                    null,
                    request.SubjectEmail,
                    now);

                if (!string.IsNullOrWhiteSpace(request.SubjectPhone))
                {
                    var phone = SubjectPhone.Create(
                        UlidId.NewUlid(),
                        request.SubjectPhone,
                        PhoneType.Mobile,
                        true,
                        now);
                    subject.AddPhone(phone);
                }

                await subjectsUnitOfWork.Subjects.AddAsync(subject, cancellationToken);
            }

            // Save with deferred dispatch - events go to outbox only
            await subjectsUnitOfWork.SaveChangesAsync(true, cancellationToken);

            // 2. Create Order
            var orderId = UlidId.NewUlid();
            var order = Order.Create(
                orderId,
                subject.Id,
                request.CustomerId,
                request.PolicySnapshotId,
                now);

            await ordersUnitOfWork.Orders.AddAsync(order, cancellationToken);
            await ordersUnitOfWork.SaveChangesAsync(true, cancellationToken);

            // 3. Create IntakeSession
            var resumeToken = GenerateResumeToken();
            var policySnapshot = PolicySnapshot.Create(
                request.PolicySnapshotId,
                "v1",
                now);

            var session = IntakeSession.Invite(
                UlidId.NewUlid(),
                orderId,
                subject.Id,
                request.CustomerId,
                policySnapshot,
                resumeToken,
                now,
                TimeSpan.FromHours(DefaultTimeToLiveHours));

            await intakeSessionsUnitOfWork.IntakeSessions.AddAsync(session, cancellationToken);
            await intakeSessionsUnitOfWork.SaveChangesAsync(true, cancellationToken);

            // Commit the transaction - all three modules' changes are now durable
            scope.Complete();

            // Events are in the outbox - OutboxProcessor will dispatch them
            // This ensures IntakeSessionInvited etc. only fire after commit

            return Result.Success(new CreateOrderWithIntakeResult(
                subject.Id,
                subjectWasExisting,
                orderId,
                session.Id,
                session.ResumeToken,
                session.ExpiresAt));
        }
        catch (Exception ex)
        {
            // TransactionScope automatically rolls back if Complete() not called
            return Result.Fail<CreateOrderWithIntakeResult>(
                $"Failed to create order with intake: {ex.Message}");
        }
    }

    private static string GenerateResumeToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}