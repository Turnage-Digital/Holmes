using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions.Queries;
using MediatR;

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

public sealed class GetOrderCompletionStatusQueryHandler(
    IServiceRequestQueries serviceRequestQueries
) : IRequestHandler<GetOrderCompletionStatusQuery, Result<OrderCompletionStatus>>
{
    public async Task<Result<OrderCompletionStatus>> Handle(
        GetOrderCompletionStatusQuery request,
        CancellationToken cancellationToken
    )
    {
        var status = await serviceRequestQueries.GetOrderCompletionStatusAsync(
            request.OrderId.ToString(), cancellationToken);

        return Result.Success(new OrderCompletionStatus(
            status.AllCompleted,
            status.TotalServices,
            status.CompletedServices,
            status.PendingServices,
            0, // InProgress is included in Pending count in the DTO
            status.FailedServices));
    }
}