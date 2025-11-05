using System.Text.Json.Serialization;

namespace Holmes.Core.Domain.ValueObjects;

public record Entry<TType>
    where TType : Enum
{
    [JsonPropertyName("type")]
    public TType Type { get; set; } = default!;

    [JsonPropertyName("on")]
    public DateTime On { get; set; }

    [JsonPropertyName("by")]
    public string By { get; set; } = null!;

    [JsonPropertyName("bag")]
    public object? Bag { get; set; }
}

public record HistoryPage<TType>
    where TType : Enum
{
    public Entry<TType>[] Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int Total { get; init; }
}