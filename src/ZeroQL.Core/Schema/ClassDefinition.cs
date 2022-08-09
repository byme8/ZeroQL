namespace ZeroQL.Core.Schema;

public class ClassDefinition
{
    public ClassDefinition(string name, FieldDefinition[] properties)
    {
        Name = name;
        Properties = properties;
    }

    public string Name { get; init; }

    public FieldDefinition[] Properties { get; init; }
}