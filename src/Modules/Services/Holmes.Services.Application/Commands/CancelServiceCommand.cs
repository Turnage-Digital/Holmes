using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Services.Application.Commands;

public sealed record CancelServiceCommand(
    UlidId ServiceId,
    string Reason,
    DateTimeOffset CanceledAt
) : RequestBase<Result>;