using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Contracts.Dtos;

namespace Holmes.IntakeSessions.Application.Queries;

public sealed record GetIntakeSessionBootstrapQuery(
    UlidId IntakeSessionId,
    string ResumeToken
) : RequestBase<IntakeSessionBootstrapDto?>;