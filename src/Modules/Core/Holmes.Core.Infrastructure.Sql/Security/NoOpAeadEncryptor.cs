using System;
using System.Threading;
using System.Threading.Tasks;
using Holmes.Core.Domain.Security;

namespace Holmes.Core.Infrastructure.Sql.Security;

/// <summary>
/// Development-only AEAD encryptor that returns payloads unchanged.
/// Replace with a real implementation when secrets management is available.
/// </summary>
public sealed class NoOpAeadEncryptor : IAeadEncryptor
{
    public ValueTask<byte[]> EncryptAsync(
        ReadOnlyMemory<byte> plaintext,
        ReadOnlyMemory<byte>? associatedData = null,
        CancellationToken cancellationToken = default
    )
    {
        return ValueTask.FromResult(plaintext.ToArray());
    }

    public ValueTask<byte[]> DecryptAsync(
        ReadOnlyMemory<byte> ciphertext,
        ReadOnlyMemory<byte>? associatedData = null,
        CancellationToken cancellationToken = default
    )
    {
        return ValueTask.FromResult(ciphertext.ToArray());
    }
}
