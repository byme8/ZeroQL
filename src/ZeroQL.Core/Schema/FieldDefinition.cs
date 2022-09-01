namespace ZeroQL.Schema;

public class FieldDefinition
{
    public FieldDefinition(string name, TypeDefinition typeDefinition, ArgumentDefinition[] arguments)
    {
        Name = name;
        Arguments = arguments;
        TypeDefinition = typeDefinition;
    }

    public string Name { get; init; }

    public ArgumentDefinition[] Arguments { get; init; }

    public TypeDefinition TypeDefinition { get; init; }
}