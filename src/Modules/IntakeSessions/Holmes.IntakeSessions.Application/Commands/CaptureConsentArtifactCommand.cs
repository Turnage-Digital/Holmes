using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed record CaptureConsentArtifactCommand(
    UlidId IntakeSessionId,
    string MimeType,
    string SchemaVersion,
    byte[] Payload,
    DateTimeOffset CapturedAt,
    IReadOnlyDictionary<string, string>? Metadata = null
) : RequestBase<Result<ConsentArtifactDescriptor>>;