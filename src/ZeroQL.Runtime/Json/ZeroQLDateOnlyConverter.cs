using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroQL.Json;

#if NETSTANDARD
public class ZeroQLDateOnlyConverter : JsonConverter<DateTime>
#else
public class ZeroQLDateOnlyConverter : JsonConverter<DateOnly>
#endif
{
    private readonly string serializationFormat = "yyyy-MM-dd";
#if NETSTANDARD
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();

        return DateTime.ParseExact(value!, serializationFormat, CultureInfo.InvariantCulture);
    }

     public override void Write(Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options)
    {
        var text = value.ToString(serializationFormat);
        writer.WriteStringValue(text);
    }
#else
    public override DateOnly Read(
        ref Utf8JsonReader reader,
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
#endif
}