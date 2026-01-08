using System.Security.Cryptography;
using System.Text.Json;
using Holmes.Core.Contracts.Security;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.IntakeSessions.Infrastructure.Sql;

public sealed class DatabaseAuthorizationArtifactStore(
    IntakeSessionsDbContext dbContext,
    IAeadEncryptor encryptor
) : IAuthorizationArtifactStore
{
    public async Task<AuthorizationArtifactDescriptor> SaveAsync(
        AuthorizationArtifactWriteRequest request,
        Stream payload,
        CancellationToken cancellationToken
    )
    {
        using var buffer = new MemoryStream();
        await payload.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var encrypted = await encryptor.EncryptAsync(bytes, cancellationToken: cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var entity = new AuthorizationArtifactDb
        {
            ArtifactId = request.ArtifactId.ToString(),
            OrderId = request.OrderId.ToString(),
            SubjectId = request.SubjectId.ToString(),
            MimeType = request.MimeType,
            Length = bytes.LongLength,
            Hash = hash,
            HashAlgorithm = "SHA256",
            SchemaVersion = request.SchemaVersion,
            CreatedAt = request.CapturedAt,
            Payload = encrypted,
            MetadataJson = JsonSerializer.Serialize(request.Metadata)
        };

        dbContext.AuthorizationArtifacts.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthorizationArtifactDescriptor(
            request.ArtifactId,
            request.MimeType,
            bytes.LongLength,
            hash,
            entity.HashAlgorithm,
            request.SchemaVersion,
            entity.CreatedAt);
    }

    public async Task<AuthorizationArtifactStream?> GetAsync(UlidId artifactId, CancellationToken cancellationToken)
    {
        var id = artifactId.ToString();
        var entity = await dbContext.AuthorizationArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ArtifactId == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var decrypted = await encryptor.DecryptAsync(entity.Payload, cancellationToken: cancellationToken);
        var descriptor = new AuthorizationArtifactDescriptor(
            artifactId,
            entity.MimeType,
            entity.Length,
            entity.Hash,
            entity.HashAlgorithm,
            entity.SchemaVersion,
            entity.CreatedAt);

        return new AuthorizationArtifactStream(descriptor, new MemoryStream(decrypted, false));
    }

    public async Task<bool> ExistsAsync(UlidId artifactId, CancellationToken cancellationToken)
    {
        var id = artifactId.ToString();
        return await dbContext.AuthorizationArtifacts.AnyAsync(x => x.ArtifactId == id, cancellationToken);
    }
}
