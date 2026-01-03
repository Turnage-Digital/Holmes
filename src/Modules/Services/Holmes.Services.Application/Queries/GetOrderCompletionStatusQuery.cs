using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Services.Application.Queries;

public sealed record OrderCompletionStatus(
    bool AllCompleted,
    int TotalServices,
    int CompletedServices,
    int PendingServices,
    int InProgressServices,
    int FailedServices
);

public sealed record GetOrderCompletionStatusQuery(
    UlidId OrderId
) : RequestBase<Result<OrderCompletionStatus>>;