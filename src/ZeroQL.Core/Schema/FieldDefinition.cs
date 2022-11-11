namespace ZeroQL.Schema;

public class FieldDefinition
{
    public FieldDefinition(string name, TypeDefinition typeDefinition, ArgumentDefinition[] arguments, string? defaultValue)
    {
        Name = name;
        Arguments = arguments;
        TypeDefinition = typeDefinition;
        DefaultValue = defaultValue;
    }

    public string Name { get; init; }

    public ArgumentDefinition[] Arguments { get; init; }

    public TypeDefinition TypeDefinition { get; init; }

    public string? DefaultValue { get; init; }
}