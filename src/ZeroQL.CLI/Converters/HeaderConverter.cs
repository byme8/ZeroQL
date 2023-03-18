using CliFx.Exceptions;
using CliFx.Extensibility;

namespace ZeroQL.CLI.Converters;

public class HeaderConverter : BindingConverter<KeyValuePair<string, string>?>
{
    public override KeyValuePair<string, string>? Convert(string? rawValue)
    {
        if (rawValue is null)
        {
            return null;
        }

        var split = rawValue.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (split.Length == 2)
        {
            return new KeyValuePair<string, string>(split[0], split[1]);
        }
        
        throw new CliFxException($"Invalid header format: {rawValue}");
    }
}