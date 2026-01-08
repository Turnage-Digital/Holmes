using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed class CaptureAuthorizationArtifactCommandHandler(
    IAuthorizationArtifactStore artifactStore,
    IIntakeSessionsUnitOfWork unitOfWork
)
    : IRequestHandler<CaptureAuthorizationArtifactCommand, Result<AuthorizationArtifactDescriptor>>
{
    public async Task<Result<AuthorizationArtifactDescriptor>> Handle(
        CaptureAuthorizationArtifactCommand request,
        CancellationToken cancellationToken
    )
    {
        var repository = unitOfWork.IntakeSessions;
        var session = await repository.GetByIdAsync(request.IntakeSessionId, cancellationToken);
        if (session is null)
        {
            return Result.Fail<AuthorizationArtifactDescriptor>(ResultErrors.NotFound);
        }

        if (request.Payload.Length == 0)
        {
            return Result.Fail<AuthorizationArtifactDescriptor>(ResultErrors.Validation);
        }

        var artifactId = UlidId.NewUlid();
        await using var stream = new MemoryStream(request.Payload, false);
        var writeRequest = new AuthorizationArtifactWriteRequest(
            artifactId,
            session.OrderId,
            session.SubjectId,
            request.MimeType,
            request.SchemaVersion,
            request.CapturedAt,
            request.Metadata ?? new Dictionary<string, string>());

        var descriptor = await artifactStore.SaveAsync(writeRequest, stream, cancellationToken);
        var pointer = AuthorizationArtifactPointer.Create(
            descriptor.ArtifactId,
            descriptor.MimeType,
            descriptor.Length,
            descriptor.Hash,
            descriptor.HashAlgorithm,
            descriptor.SchemaVersion,
            descriptor.CreatedAt);

        session.CaptureAuthorization(pointer);
        await repository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(descriptor);
    }
}
