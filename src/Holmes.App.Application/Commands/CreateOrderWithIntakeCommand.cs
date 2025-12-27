using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using MediatR;
using Holmes.Subjects.Application.Abstractions.Commands;

namespace Holmes.App.Application.Commands;

/// <summary>
///     Requests Subject creation/reuse and triggers Order creation via integration events.
///     Intake invites are issued asynchronously to avoid cross-module transactions.
/// </summary>
public sealed record CreateOrderWithIntakeCommand(
    string SubjectEmail,
    string? SubjectPhone,
    UlidId CustomerId,
    string PolicySnapshotId
) : RequestBase<Result<CreateOrderWithIntakeResult>>;

public sealed record CreateOrderWithIntakeResult(
    UlidId SubjectId,
    bool SubjectWasExisting,
    UlidId OrderId
);

public sealed class CreateOrderWithIntakeCommandHandler(
    ISender sender
) : IRequestHandler<CreateOrderWithIntakeCommand, Result<CreateOrderWithIntakeResult>>
{
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

        try
        {
            var subjectRequest = new RequestSubjectIntakeCommand(
                request.SubjectEmail.Trim(),
                request.SubjectPhone?.Trim(),
                request.CustomerId,
                request.PolicySnapshotId,
                DateTimeOffset.UtcNow)
            {
                UserId = request.UserId
            };

            var result = await sender.Send(subjectRequest, cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Fail<CreateOrderWithIntakeResult>(result.Error ?? "Failed to request subject intake.");
            }

            return Result.Success(new CreateOrderWithIntakeResult(
                result.Value.SubjectId,
                result.Value.SubjectWasExisting,
                result.Value.OrderId));
        }
        catch (Exception ex)
        {
            return Result.Fail<CreateOrderWithIntakeResult>(
                $"Failed to create order with intake: {ex.Message}");
        }
    }
}
