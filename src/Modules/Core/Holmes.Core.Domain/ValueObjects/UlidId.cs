using System;

namespace Holmes.Core.Domain.ValueObjects;

public readonly record struct UlidId
{
    public Ulid Value { get; }

    private UlidId(Ulid value)
    {
        Value = value;
    }

    public static UlidId NewUlid() => new(Ulid.NewUlid());

    public static UlidId FromUlid(Ulid value) => new(value);

    public static bool TryParse(string? value, out UlidId ulid)
    {
        if (!string.IsNullOrWhiteSpace(value) && Ulid.TryParse(value, out var parsed))
        {
            ulid = new UlidId(parsed);
            return true;
        }

        ulid = default;
        return false;
    }

    public static UlidId Parse(string value)
    {
        if (TryParse(value, out var ulid))
        {
            return ulid;
        }

        throw new FormatException($"Value '{value}' is not a valid ULID.");
    }

    public override string ToString() => Value.ToString();

    public static implicit operator string(UlidId id) => id.ToString();
    public static implicit operator Ulid(UlidId id) => id.Value;
}
