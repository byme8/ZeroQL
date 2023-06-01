using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZeroQL;

public record ZeroQLScalar
{
    public string Value { get; set; }

    public override string ToString() => Value;
}

public class ZeroQLScalarJsonConverter<TScalar> : JsonConverter<TScalar>
    where TScalar : ZeroQLScalar, new()
{
    public override TScalar? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value is null)
        {
            return null;
        }

        var scalar = new TScalar
        {
            Value = value
        };

        return scalar;
    }

    public override void Write(Utf8JsonWriter writer, TScalar value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}