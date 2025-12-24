using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed record CaptureConsentArtifactCommand(
    UlidId IntakeSessionId,
    string MimeType,
    string SchemaVersion,
    byte[] Payload,
    DateTimeOffset CapturedAt,
    IReadOnlyDictionary<string, string>? Metadata = null
) : RequestBase<Result<ConsentArtifactDescriptor>>;

public sealed class CaptureConsentArtifactCommandHandler(
    IConsentArtifactStore artifactStore,
    IIntakeUnitOfWork unitOfWork
)
    : IRequestHandler<CaptureConsentArtifactCommand, Result<ConsentArtifactDescriptor>>
{
    public async Task<Result<ConsentArtifactDescriptor>> Handle(
        CaptureConsentArtifactCommand request,
        CancellationToken cancellationToken
    )
    {
        var repository = unitOfWork.IntakeSessions;
        var session = await repository.GetByIdAsync(request.IntakeSessionId, cancellationToken);
        if (session is null)
        {
            return Result.Fail<ConsentArtifactDescriptor>($"Intake session '{request.IntakeSessionId}' not found.");
        }

        if (request.Payload.Length == 0)
        {
            return Result.Fail<ConsentArtifactDescriptor>("Consent payload cannot be empty.");
        }

        var artifactId = UlidId.NewUlid();
        await using var stream = new MemoryStream(request.Payload, false);
        var writeRequest = new ConsentArtifactWriteRequest(
            artifactId,
            session.OrderId,
            session.SubjectId,
            request.MimeType,
            request.SchemaVersion,
            request.CapturedAt,
            request.Metadata ?? new Dictionary<string, string>());

        var descriptor = await artifactStore.SaveAsync(writeRequest, stream, cancellationToken);
        var pointer = ConsentArtifactPointer.Create(
            descriptor.ArtifactId,
            descriptor.MimeType,
            descriptor.Length,
            descriptor.Hash,
            descriptor.HashAlgorithm,
            descriptor.SchemaVersion,
            descriptor.CreatedAt);

        session.CaptureConsent(pointer);
        await repository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(descriptor);
    }
}