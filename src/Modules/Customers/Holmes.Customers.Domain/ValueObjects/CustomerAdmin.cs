using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Domain.ValueObjects;

public sealed class CustomerAdmin : ValueObject
{
    public CustomerAdmin(UlidId userId, UlidId assignedBy, DateTimeOffset assignedAt)
    {
        UserId = userId;
        AssignedBy = assignedBy;
        AssignedAt = assignedAt;
    }

    public UlidId UserId { get; }

    public UlidId AssignedBy { get; }

    public DateTimeOffset AssignedAt { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
    }
}