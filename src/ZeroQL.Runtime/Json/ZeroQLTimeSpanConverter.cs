using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace ZeroQL.Json;

public class ZeroQLTimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return XmlConvert.ToTimeSpan(value!);
    }

    public override void Write(Utf8JsonWriter writer,
        TimeSpan value,
        JsonSerializerOptions options)
    {
        var text = XmlConvert.ToString(value);
        writer.WriteStringValue(text);
    }
}