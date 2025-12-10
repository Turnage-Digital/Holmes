namespace Holmes.Core.Application.Abstractions.Security;

/// <summary>
///     Authenticated Encryption with Associated Data (AEAD) encryptor interface.
///     Used for field-level encryption of PII (SSN, sensitive answers, etc.).
/// </summary>
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