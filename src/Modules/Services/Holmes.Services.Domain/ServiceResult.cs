using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Services.Domain;

/// <summary>
///     Represents the normalized result of a service.
/// </summary>
public sealed class ServiceResult
{
    private ServiceResult()
    {
    }

    public UlidId Id { get; private set; }
    public ServiceResultStatus Status { get; private set; }
    public IReadOnlyList<NormalizedRecord> Records { get; private set; } = [];
    public string? RawResponseHash { get; private set; }
    public string? VendorReferenceId { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? NormalizedAt { get; private set; }

    public static ServiceResult Create(
        UlidId id,
        ServiceResultStatus status,
        IReadOnlyList<NormalizedRecord> records,
        string? rawResponseHash,
        string? vendorReferenceId,
        DateTimeOffset receivedAt
    )
    {
        return new ServiceResult
        {
            Id = id,
            Status = status,
            Records = records,
            RawResponseHash = rawResponseHash,
            VendorReferenceId = vendorReferenceId,
            ReceivedAt = receivedAt,
            NormalizedAt = DateTimeOffset.UtcNow
        };
    }

    public static ServiceResult Clear(UlidId id, string? vendorReferenceId, DateTimeOffset receivedAt)
    {
        return Create(id, ServiceResultStatus.Clear, [], null, vendorReferenceId, receivedAt);
    }

    public static ServiceResult Hit(
        UlidId id,
        IReadOnlyList<NormalizedRecord> records,
        string? rawResponseHash,
        string? vendorReferenceId,
        DateTimeOffset receivedAt
    )
    {
        return Create(id, ServiceResultStatus.Hit, records, rawResponseHash, vendorReferenceId, receivedAt);
    }

    public static ServiceResult UnableToVerify(UlidId id, string? vendorReferenceId, DateTimeOffset receivedAt)
    {
        return Create(id, ServiceResultStatus.UnableToVerify, [], null, vendorReferenceId, receivedAt);
    }

    public static ServiceResult Error(UlidId id, string? vendorReferenceId, DateTimeOffset receivedAt)
    {
        return Create(id, ServiceResultStatus.Error, [], null, vendorReferenceId, receivedAt);
    }

    public static ServiceResult Rehydrate(
        UlidId id,
        ServiceResultStatus status,
        IReadOnlyList<NormalizedRecord> records,
        string? rawResponseHash,
        string? vendorReferenceId,
        DateTimeOffset receivedAt,
        DateTimeOffset? normalizedAt
    )
    {
        return new ServiceResult
        {
            Id = id,
            Status = status,
            Records = records,
            RawResponseHash = rawResponseHash,
            VendorReferenceId = vendorReferenceId,
            ReceivedAt = receivedAt,
            NormalizedAt = normalizedAt
        };
    }
}