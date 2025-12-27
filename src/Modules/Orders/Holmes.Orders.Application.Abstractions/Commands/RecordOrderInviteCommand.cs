using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Abstractions.Commands;

public sealed record RecordOrderInviteCommand(
    UlidId OrderId,
    UlidId IntakeSessionId,
    DateTimeOffset InvitedAt,
    string? Reason
) : RequestBase<Result>, ISkipUserAssignment;
