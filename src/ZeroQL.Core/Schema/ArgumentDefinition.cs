namespace ZeroQL.Schema;

public class ArgumentDefinition
{
    public ArgumentDefinition(string name, string typeName)
    {
        Name = name;
        TypeName = typeName;
    }

    public string Name { get; init; }

    public string TypeName { get; init; }
}