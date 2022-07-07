namespace LinqQL.Core.Schema;

public class FieldDefinition
{
    public string Name { get; set; }

    public ArgumentDefinition[] Arguments { get; set; }

    public TypeDefinition TypeDefinition { get; set; }
}