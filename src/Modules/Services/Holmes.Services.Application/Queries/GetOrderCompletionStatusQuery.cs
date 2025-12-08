using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
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
    IServicesUnitOfWork unitOfWork
) : IRequestHandler<GetOrderCompletionStatusQuery, Result<OrderCompletionStatus>>
{
    public async Task<Result<OrderCompletionStatus>> Handle(
        GetOrderCompletionStatusQuery request,
        CancellationToken cancellationToken
    )
    {
        var services = await unitOfWork.ServiceRequests.GetByOrderIdAsync(
            request.OrderId, cancellationToken);

        if (services.Count == 0)
        {
            return Result.Success(new OrderCompletionStatus(
                true,
                0,
                0,
                0,
                0,
                0));
        }

        var completed = services.Count(s => s.Status == ServiceStatus.Completed || s.Status == ServiceStatus.Canceled);
        var pending = services.Count(s => s.Status == ServiceStatus.Pending);
        var inProgress =
            services.Count(s => s.Status == ServiceStatus.Dispatched || s.Status == ServiceStatus.InProgress);
        var failed = services.Count(s => s.Status == ServiceStatus.Failed);

        return Result.Success(new OrderCompletionStatus(
            completed == services.Count,
            services.Count,
            completed,
            pending,
            inProgress,
            failed));
    }
}