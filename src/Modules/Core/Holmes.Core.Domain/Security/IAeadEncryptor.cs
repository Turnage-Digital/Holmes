namespace Holmes.Core.Domain.Security;

public interface IAeadEncryptor
{
    ValueTask<byte[]> EncryptAsync(
        ReadOnlyMemory<byte> plaintext,
        ReadOnlyMemory<byte>? associatedData = null,
        CancellationToken cancellationToken = default
    );

    ValueTask<byte[]> DecryptAsync(
        ReadOnlyMemory<byte> ciphertext,
        ReadOnlyMemory<byte>? associatedData = null,
        CancellationToken cancellationToken = default
    );
}