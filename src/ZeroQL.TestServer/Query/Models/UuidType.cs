using HotChocolate.Language;

namespace ZeroQL.TestServer.Query.Models;

public record Uuid(string Value);
public sealed class UuidType : ScalarType<Uuid, StringValueNode>
{
    public UuidType()
        : base("uuid")
    {
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is Uuid uuid)
        {
            return new StringValueNode(uuid.Value);
        }

        return new NullValueNode(null);
    }

    protected override Uuid ParseLiteral(StringValueNode valueSyntax)
    {
        return new Uuid(valueSyntax.Value);
    }

    protected override StringValueNode ParseValue(Uuid runtimeValue)
    {
        return new StringValueNode(runtimeValue.Value);
    }

    public override object? Serialize(object? runtimeValue)
    {
        if (runtimeValue is Uuid uuid)
        {
            return uuid.Value;
        }

        return null;
    }
}