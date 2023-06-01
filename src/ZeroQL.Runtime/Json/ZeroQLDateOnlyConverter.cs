using System;
using System.Text.Json;
using System.Text.Json.Serialization;

#if !NETSTANDARD
namespace ZeroQL.Json;

public class ZeroQLDateOnlyConverter : JsonConverter<DateOnly>
{
    private readonly string serializationFormat = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return DateOnly.Parse(value!);
    }

    public override void Write(Utf8JsonWriter writer,
        DateOnly value,
        JsonSerializerOptions options)
    {
        var text = value.ToString(serializationFormat);
        writer.WriteStringValue(text);
    }
}
#endif