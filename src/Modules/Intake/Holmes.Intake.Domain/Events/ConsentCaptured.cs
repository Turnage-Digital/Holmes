using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain.ValueObjects;
using MediatR;

namespace Holmes.Intake.Domain.Events;

public sealed record ConsentCaptured(
    UlidId IntakeSessionId,
    UlidId OrderId,
    ConsentArtifactPointer Artifact
) : INotification;