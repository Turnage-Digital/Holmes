using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;

namespace Holmes.Services.Application.Abstractions.Commands;

public sealed record CreateServiceCommand(
    UlidId OrderId,
    UlidId CustomerId,
    string ServiceTypeCode,
    int Tier,
    ServiceScope? Scope,
    UlidId? CatalogSnapshotId,
    DateTimeOffset CreatedAt
) : RequestBase<Result<UlidId>>, ISkipUserAssignment;