using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Domain.Events;

public sealed record ConsentCaptured(
    UlidId IntakeSessionId,
    UlidId OrderId,
    ConsentArtifactPointer Artifact
) : INotification;