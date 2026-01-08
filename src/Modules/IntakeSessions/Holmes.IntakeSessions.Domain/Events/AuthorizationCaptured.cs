using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Domain.Events;

public sealed record AuthorizationCaptured(
    UlidId IntakeSessionId,
    UlidId OrderId,
    AuthorizationArtifactPointer Artifact
) : INotification;
