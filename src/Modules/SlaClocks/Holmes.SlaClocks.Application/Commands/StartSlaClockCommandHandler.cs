using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Application.Abstractions.Services;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Application.Commands;

public sealed class StartSlaClockCommandHandler(
    ISlaClocksUnitOfWork unitOfWork,
    IBusinessCalendarService calendarService
) : IRequestHandler<StartSlaClockCommand, Result>
{
    // Default SLA targets (from customer service agreements; these are fallbacks)
    private const int DefaultIntakeDays = 1;
    private const int DefaultFulfillmentDays = 3;
    private const int DefaultOverallDays = 5;
    private const decimal DefaultAtRiskThreshold = 0.80m;

    public async Task<Result> Handle(StartSlaClockCommand request, CancellationToken cancellationToken)
    {
        // Check if clock of this kind already exists for this order
        var existing = await unitOfWork.SlaClocks.GetByOrderIdAndKindAsync(
            request.OrderId, request.Kind, cancellationToken);

        if (existing is not null && !existing.IsTerminal)
        {
            // Clock already running - no-op
            return Result.Success();
        }

        var targetDays = request.TargetBusinessDays ?? GetDefaultTargetDays(request.Kind);
        var atRiskThreshold = request.AtRiskThresholdPercent ?? DefaultAtRiskThreshold;

        var deadlineAt = calendarService.AddBusinessDays(request.StartedAt, targetDays, request.CustomerId);
        var atRiskThresholdAt = calendarService.CalculateAtRiskThreshold(
            request.StartedAt, deadlineAt, atRiskThreshold);

        var clock = SlaClock.Start(
            UlidId.NewUlid(),
            request.OrderId,
            request.CustomerId,
            request.Kind,
            request.StartedAt,
            deadlineAt,
            atRiskThresholdAt,
            targetDays,
            atRiskThreshold);

        unitOfWork.SlaClocks.Add(clock);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static int GetDefaultTargetDays(ClockKind kind)
    {
        return kind switch
        {
            ClockKind.Intake => DefaultIntakeDays,
            ClockKind.Fulfillment => DefaultFulfillmentDays,
            ClockKind.Overall => DefaultOverallDays,
            _ => DefaultOverallDays
        };
    }
}