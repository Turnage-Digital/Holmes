using System.Text.Json;
using System.Text.Json.Serialization;

namespace Holmes.Core.Domain.ValueObjects;

/// <summary>
///     JSON converter that serializes UlidId as a plain string rather than an object.
/// </summary>
public sealed class UlidIdJsonConverter : JsonConverter<UlidId>
{
    public override UlidId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return default;
        }

        return UlidId.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, UlidId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}