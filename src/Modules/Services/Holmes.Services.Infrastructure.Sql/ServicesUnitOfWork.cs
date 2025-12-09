using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Infrastructure.Sql;

public sealed class ServicesUnitOfWork(
    ServicesDbContext dbContext,
    IMediator mediator,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null)
    : UnitOfWork<ServicesDbContext>(dbContext, mediator, eventStore, serializer, tenantContext), IServicesUnitOfWork
{
    private readonly Lazy<IServiceRequestRepository> _serviceRequests = new(() => new ServiceRequestRepository(dbContext));

    public IServiceRequestRepository ServiceRequests => _serviceRequests.Value;
}