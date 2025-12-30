using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Contracts.Dtos;

namespace Holmes.Services.Application.Queries;

public sealed record GetServicesByOrderQuery(
    UlidId OrderId
) : RequestBase<Result<IReadOnlyList<ServiceSummaryDto>>>;