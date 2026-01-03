using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;

namespace Holmes.Services.Application.Commands;

public sealed record CreateServiceCommand(
    UlidId OrderId,
    UlidId CustomerId,
    string ServiceTypeCode,
    int Tier,
    ServiceScope? Scope,
    UlidId? CatalogSnapshotId,
    DateTimeOffset CreatedAt
) : RequestBase<Result<UlidId>>;