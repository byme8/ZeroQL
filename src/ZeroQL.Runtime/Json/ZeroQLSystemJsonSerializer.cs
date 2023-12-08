using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroQL.Json;

public class ZeroQLSystemJsonSerializer(JsonSerializerOptions options) : IZeroQLSerializer
{
    public JsonSerializerOptions Options { get; } = options;

    public byte[] SerializeRaw<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, Options);
    }

    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public async Task Serialize<T>(Stream stream, T value, CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync(stream, value, Options, cancellationToken);
    }

    public T? Deserialize<T>(byte[] bytes)
    {
        return JsonSerializer.Deserialize<T>(bytes, Options);
    }

    public T? Deserialize<T>(string text)
    {
        return JsonSerializer.Deserialize<T>(text, Options);
    }

    public async Task<T?> Deserialize<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        return await JsonSerializer.DeserializeAsync<T>(stream, Options, cancellationToken);
    }
}