using Holmes.Core.Domain;
using Holmes.Services.Application.Queries;
using MediatR;

namespace Holmes.Services.Application.Queries;

public sealed class GetOrderCompletionStatusQueryHandler(
    IServiceQueries serviceQueries
) : IRequestHandler<GetOrderCompletionStatusQuery, Result<OrderCompletionStatus>>
{
    public async Task<Result<OrderCompletionStatus>> Handle(
        GetOrderCompletionStatusQuery request,
        CancellationToken cancellationToken
    )
    {
        var status = await serviceQueries.GetOrderCompletionStatusAsync(
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