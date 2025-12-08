using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Domain;

public sealed class SubjectPhone
{
    private SubjectPhone()
    {
    }

    public UlidId Id { get; private set; }

    public string PhoneNumber { get; private set; } = null!;

    public PhoneType Type { get; private set; }

    public bool IsPrimary { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static SubjectPhone Create(
        UlidId id,
        string phoneNumber,
        PhoneType type,
        bool isPrimary,
        DateTimeOffset createdAt
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        return new SubjectPhone
        {
            Id = id,
            PhoneNumber = phoneNumber,
            Type = type,
            IsPrimary = isPrimary,
            CreatedAt = createdAt
        };
    }

    public static SubjectPhone Rehydrate(
        UlidId id,
        string phoneNumber,
        PhoneType type,
        bool isPrimary,
        DateTimeOffset createdAt
    )
    {
        return new SubjectPhone
        {
            Id = id,
            PhoneNumber = phoneNumber,
            Type = type,
            IsPrimary = isPrimary,
            CreatedAt = createdAt
        };
    }
}

public enum PhoneType
{
    Mobile = 0,
    Home = 1,
    Work = 2
}