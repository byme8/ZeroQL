using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroQL;

public interface IZeroQLSerializer
{
    byte[] SerializeRaw<T>(T value);
    
    string Serialize<T>(T value);

    Task Serialize<T>(Stream stream, T value, CancellationToken cancellationToken = default);

    T? Deserialize<T>(byte[] bytes);
    
    T? Deserialize<T>(string text);

    Task<T?> Deserialize<T>(Stream stream, CancellationToken cancellationToken = default);
}