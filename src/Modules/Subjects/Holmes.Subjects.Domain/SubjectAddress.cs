using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Domain;

public sealed class SubjectAddress
{
    private SubjectAddress()
    {
    }

    public UlidId Id { get; private set; }

    public string Street1 { get; private set; } = null!;

    public string? Street2 { get; private set; }

    public string City { get; private set; } = null!;

    public string State { get; private set; } = null!;

    public string PostalCode { get; private set; } = null!;

    public string Country { get; private set; } = null!;

    public string? CountyFips { get; private set; }

    public DateOnly FromDate { get; private set; }

    public DateOnly? ToDate { get; private set; }

    public bool IsCurrent => ToDate is null;

    public AddressType Type { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static SubjectAddress Create(
        UlidId id,
        string street1,
        string? street2,
        string city,
        string state,
        string postalCode,
        string country,
        string? countyFips,
        DateOnly fromDate,
        DateOnly? toDate,
        AddressType type,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(street1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);

        return new SubjectAddress
        {
            Id = id,
            Street1 = street1,
            Street2 = street2,
            City = city,
            State = state.ToUpperInvariant(),
            PostalCode = postalCode,
            Country = country.ToUpperInvariant(),
            CountyFips = countyFips,
            FromDate = fromDate,
            ToDate = toDate,
            Type = type,
            CreatedAt = createdAt
        };
    }

    public static SubjectAddress Rehydrate(
        UlidId id,
        string street1,
        string? street2,
        string city,
        string state,
        string postalCode,
        string country,
        string? countyFips,
        DateOnly fromDate,
        DateOnly? toDate,
        AddressType type,
        DateTimeOffset createdAt)
    {
        return new SubjectAddress
        {
            Id = id,
            Street1 = street1,
            Street2 = street2,
            City = city,
            State = state,
            PostalCode = postalCode,
            Country = country,
            CountyFips = countyFips,
            FromDate = fromDate,
            ToDate = toDate,
            Type = type,
            CreatedAt = createdAt
        };
    }

    public void SetCountyFips(string countyFips)
    {
        CountyFips = countyFips;
    }
}

public enum AddressType
{
    Residential = 0,
    Mailing = 1,
    Business = 2
}
