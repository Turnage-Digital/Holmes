using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Services.Contracts.Dtos;

namespace Holmes.Services.Application.Queries;

public sealed record ListServiceTypesQuery : RequestBase<Result<IReadOnlyCollection<ServiceTypeDto>>>;