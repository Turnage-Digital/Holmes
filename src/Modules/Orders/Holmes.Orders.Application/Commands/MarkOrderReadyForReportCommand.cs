using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Commands;

/// <summary>
///     Transitions an order from FulfillmentInProgress to ReadyForReport.
///     Called when all required Services for the order have completed.
/// </summary>
public sealed record MarkOrderReadyForReportCommand(
    UlidId OrderId,
    DateTimeOffset ReadyAt,
    string? Reason
) : RequestBase<Result>;
