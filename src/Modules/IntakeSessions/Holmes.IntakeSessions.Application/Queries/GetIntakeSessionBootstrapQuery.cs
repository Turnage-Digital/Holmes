using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Abstractions.Dtos;

namespace Holmes.IntakeSessions.Application.Queries;

public sealed record GetIntakeSessionBootstrapQuery(
    UlidId IntakeSessionId,
    string ResumeToken
) : RequestBase<IntakeSessionBootstrapDto?>;