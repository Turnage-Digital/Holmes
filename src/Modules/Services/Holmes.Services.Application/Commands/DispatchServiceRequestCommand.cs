using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions.Vendors;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed record DispatchServiceRequestCommand(
    UlidId ServiceRequestId,
    DateTimeOffset DispatchedAt
) : RequestBase<Result>;

public sealed class DispatchServiceRequestCommandHandler(
    IServicesUnitOfWork unitOfWork,
    IVendorAdapterFactory vendorAdapterFactory
) : IRequestHandler<DispatchServiceRequestCommand, Result>
{
    public async Task<Result> Handle(
        DispatchServiceRequestCommand request,
        CancellationToken cancellationToken
    )
    {
        var serviceRequest = await unitOfWork.ServiceRequests.GetByIdAsync(
            request.ServiceRequestId, cancellationToken);

        if (serviceRequest is null)
        {
            return Result.Fail($"Service request {request.ServiceRequestId} not found");
        }

        if (serviceRequest.Status != ServiceStatus.Pending)
        {
            return Result.Fail("Service request is not in Pending status");
        }

        // If no vendor assigned, select one based on category
        if (string.IsNullOrEmpty(serviceRequest.VendorCode))
        {
            var adapter = vendorAdapterFactory.GetAdapterForCategory(serviceRequest.Category);
            if (adapter is null)
            {
                return Result.Fail($"No vendor available for category {serviceRequest.Category}");
            }

            serviceRequest.AssignVendor(adapter.VendorCode, request.DispatchedAt);
        }

        // Get the adapter for the assigned vendor
        var vendorAdapter = vendorAdapterFactory.GetAdapter(serviceRequest.VendorCode!);
        if (vendorAdapter is null)
        {
            return Result.Fail($"Vendor adapter '{serviceRequest.VendorCode}' not found");
        }

        // Dispatch to vendor
        var dispatchResult = await vendorAdapter.DispatchAsync(serviceRequest, cancellationToken);
        if (!dispatchResult.Success)
        {
            serviceRequest.Fail(dispatchResult.ErrorMessage ?? "Dispatch failed", request.DispatchedAt);
            unitOfWork.ServiceRequests.Update(serviceRequest);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Fail(dispatchResult.ErrorMessage ?? "Dispatch failed");
        }

        serviceRequest.Dispatch(dispatchResult.VendorReferenceId!, request.DispatchedAt);
        unitOfWork.ServiceRequests.Update(serviceRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}