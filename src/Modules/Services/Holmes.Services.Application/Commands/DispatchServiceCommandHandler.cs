using Holmes.Core.Application;
using Holmes.Services.Contracts;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed class DispatchServiceCommandHandler(
    IServicesUnitOfWork unitOfWork,
    IVendorAdapterFactory vendorAdapterFactory
) : IRequestHandler<DispatchServiceCommand, Result>
{
    public async Task<Result> Handle(
        DispatchServiceCommand request,
        CancellationToken cancellationToken
    )
    {
        var service = await unitOfWork.Services.GetByIdAsync(
            request.ServiceId, cancellationToken);

        if (service is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        if (service.Status != ServiceStatus.Pending)
        {
            return Result.Fail(ResultErrors.Validation);
        }

        // If no vendor assigned, select one based on category
        if (string.IsNullOrEmpty(service.VendorCode))
        {
            var adapter = vendorAdapterFactory.GetAdapterForCategory(service.Category);
            if (adapter is null)
            {
                return Result.Fail(ResultErrors.Validation);
            }

            service.AssignVendor(adapter.VendorCode, request.DispatchedAt);
        }

        // Get the adapter for the assigned vendor
        var vendorAdapter = vendorAdapterFactory.GetAdapter(service.VendorCode!);
        if (vendorAdapter is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        // Dispatch to vendor
        var dispatchResult = await vendorAdapter.DispatchAsync(service, cancellationToken);
        if (!dispatchResult.Success)
        {
            service.Fail(dispatchResult.ErrorMessage ?? "Dispatch failed", request.DispatchedAt);
            unitOfWork.Services.Update(service);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Fail(dispatchResult.ErrorMessage ?? "Dispatch failed");
        }

        service.Dispatch(dispatchResult.VendorReferenceId!, request.DispatchedAt);
        unitOfWork.Services.Update(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}