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
        var metadata = BuildAuthorizationMetadata(request.Metadata, session.PolicySnapshot.Metadata);
        if (metadata is null)
        {
            return Result.Fail<AuthorizationArtifactDescriptor>(ResultErrors.Validation);
        }

        var writeRequest = new AuthorizationArtifactWriteRequest(
            artifactId,
            session.OrderId,
            session.SubjectId,
            request.MimeType,
            request.SchemaVersion,
            request.CapturedAt,
            metadata);

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

    private static Dictionary<string, string>? BuildAuthorizationMetadata(
        IReadOnlyDictionary<string, string>? requestMetadata,
        IReadOnlyDictionary<string, string> policyMetadata)
    {
        var metadata = new Dictionary<string, string>(
            requestMetadata ?? new Dictionary<string, string>(),
            StringComparer.OrdinalIgnoreCase);

        if (!TryCopyPolicyField(policyMetadata, metadata, IntakeMetadataKeys.DisclosureId) ||
            !TryCopyPolicyField(policyMetadata, metadata, IntakeMetadataKeys.DisclosureVersion) ||
            !TryCopyPolicyField(policyMetadata, metadata, IntakeMetadataKeys.DisclosureHash) ||
            !TryCopyPolicyField(policyMetadata, metadata, IntakeMetadataKeys.AuthorizationMode) ||
            !TryCopyPolicyField(policyMetadata, metadata, IntakeMetadataKeys.AuthorizationVersion) ||
            !TryCopyPolicyField(policyMetadata, metadata, IntakeMetadataKeys.AuthorizationHash))
        {
            return null;
        }

        TryCopyPolicyField(policyMetadata, metadata, IntakeMetadataKeys.DisclosureFormat);
        TryCopyPolicyField(policyMetadata, metadata, IntakeMetadataKeys.AuthorizationFormat);
        TryCopyPolicyField(policyMetadata, metadata, IntakeMetadataKeys.AuthorizationId);

        return metadata;
    }

    private static bool TryCopyPolicyField(
        IReadOnlyDictionary<string, string> policyMetadata,
        IDictionary<string, string> destination,
        string key)
    {
        if (!policyMetadata.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        destination[key] = value;
        return true;
    }
}
