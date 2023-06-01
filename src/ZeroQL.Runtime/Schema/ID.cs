using System;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace ZeroQL;

public record ID
{
    public ID(string value)
    {
        this.Value = value;
    }

    public static implicit operator ID(string value) => new ID(value);

    public static implicit operator ID(int value) => new ID(value.ToString());

    public static implicit operator ID(Guid value) => new ID(value.ToString());

    public static implicit operator string(ID id) => id.Value;
    
    public string Value { get; }

    public void Deconstruct(out string value)
    {
        value = this.Value;
    }
}

// ReSharper disable once InconsistentNaming
public class ZeroQLIDJsonConverter : JsonConverter<ID>
{
    public override ID? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value is null)
        {
            return null;
        }

        return new ID(value);
    }

    public override void Write(Utf8JsonWriter writer, ID value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}