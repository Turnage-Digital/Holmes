using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Services.Application.Abstractions.Dtos;

namespace Holmes.Services.Application.Queries;

public sealed record ListServiceTypesQuery : RequestBase<Result<IReadOnlyCollection<ServiceTypeDto>>>;