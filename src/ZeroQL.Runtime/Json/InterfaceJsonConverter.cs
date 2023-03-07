using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ZeroQL.Json;

public abstract class InterfaceJsonConverter<TValue> : JsonConverter<TValue>
{
    public override TValue? Read(ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var json = JsonSerializer.Deserialize<JsonObject>(ref reader);
        if (json is null)
        {
            return default;
        }

        if (!json.ContainsKey("__typename"))
        {
            return default;
        }

        var type = json["__typename"]!.ToString();
        var value = Deserialize(type, json);
        return value;
    }

    public abstract TValue Deserialize(string typeName, JsonObject json);

    public override void Write(
        Utf8JsonWriter writer,
        TValue value,
        JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                JsonSerializer.Serialize(writer, default!, options);
                break;
            default:
            {
                var type = value.GetType();
                JsonSerializer.Serialize(writer, value, type, options);
                break;
            }
        }
    }
}