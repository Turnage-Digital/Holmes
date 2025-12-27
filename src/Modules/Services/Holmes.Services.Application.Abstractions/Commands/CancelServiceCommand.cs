using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Services.Application.Abstractions.Commands;

public sealed record CancelServiceCommand(
    UlidId ServiceId,
    string Reason,
    DateTimeOffset CanceledAt
) : RequestBase<Result>;
