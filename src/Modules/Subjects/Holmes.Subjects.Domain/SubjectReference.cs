using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Domain;

public sealed class SubjectReference
{
    private SubjectReference()
    {
    }

    public UlidId Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? Relationship { get; private set; }

    public int? YearsKnown { get; private set; }

    public ReferenceType Type { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static SubjectReference Create(
        UlidId id,
        string name,
        string? phone,
        string? email,
        string? relationship,
        int? yearsKnown,
        ReferenceType type,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new SubjectReference
        {
            Id = id,
            Name = name,
            Phone = phone,
            Email = email,
            Relationship = relationship,
            YearsKnown = yearsKnown,
            Type = type,
            CreatedAt = createdAt
        };
    }

    public static SubjectReference Rehydrate(
        UlidId id,
        string name,
        string? phone,
        string? email,
        string? relationship,
        int? yearsKnown,
        ReferenceType type,
        DateTimeOffset createdAt)
    {
        return new SubjectReference
        {
            Id = id,
            Name = name,
            Phone = phone,
            Email = email,
            Relationship = relationship,
            YearsKnown = yearsKnown,
            Type = type,
            CreatedAt = createdAt
        };
    }
}

public enum ReferenceType
{
    Personal = 0,
    Professional = 1
}
