using Holmes.Core.Domain;

namespace Holmes.Services.Domain;

public interface IServicesUnitOfWork : IUnitOfWork
{
    IServiceRequestRepository ServiceRequests { get; }
}