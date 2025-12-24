using System.Security.Cryptography;
using System.Text.Json;
using Holmes.Core.Application.Abstractions.Security;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.IntakeSessions.Infrastructure.Sql;

public sealed class DatabaseConsentArtifactStore(
    IntakeDbContext dbContext,
    IAeadEncryptor encryptor
) : IConsentArtifactStore
{
    public async Task<ConsentArtifactDescriptor> SaveAsync(
        ConsentArtifactWriteRequest request,
        Stream payload,
        CancellationToken cancellationToken
    )
    {
        using var buffer = new MemoryStream();
        await payload.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var encrypted = await encryptor.EncryptAsync(bytes, cancellationToken: cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var entity = new ConsentArtifactDb
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

        dbContext.ConsentArtifacts.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ConsentArtifactDescriptor(
            request.ArtifactId,
            request.MimeType,
            bytes.LongLength,
            hash,
            entity.HashAlgorithm,
            request.SchemaVersion,
            entity.CreatedAt);
    }

    public async Task<ConsentArtifactStream?> GetAsync(UlidId artifactId, CancellationToken cancellationToken)
    {
        var id = artifactId.ToString();
        var entity = await dbContext.ConsentArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ArtifactId == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var decrypted = await encryptor.DecryptAsync(entity.Payload, cancellationToken: cancellationToken);
        var descriptor = new ConsentArtifactDescriptor(
            artifactId,
            entity.MimeType,
            entity.Length,
            entity.Hash,
            entity.HashAlgorithm,
            entity.SchemaVersion,
            entity.CreatedAt);

        return new ConsentArtifactStream(descriptor, new MemoryStream(decrypted, false));
    }

    public async Task<bool> ExistsAsync(UlidId artifactId, CancellationToken cancellationToken)
    {
        var id = artifactId.ToString();
        return await dbContext.ConsentArtifacts.AnyAsync(x => x.ArtifactId == id, cancellationToken);
    }
}