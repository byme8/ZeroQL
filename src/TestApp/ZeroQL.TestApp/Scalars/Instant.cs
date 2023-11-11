using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MessagePack;

namespace ZeroQL;

[MessagePackObject]
public class Instant
{
    [Key(0)]
    public DateTimeOffset DateTimeOffset { get; set; }
}

public class InstantJsonConverter : JsonConverter<Instant?>
{
    public override Instant? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }
        return new Instant { DateTimeOffset = DateTimeOffset.Parse(text) };
    }

    public override void Write(Utf8JsonWriter writer, Instant? value, JsonSerializerOptions options)
    {
        var text = value?.DateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ssZ");
        writer.WriteStringValue(text);
    }
}