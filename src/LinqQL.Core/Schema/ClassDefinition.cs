namespace LinqQL.Core.Schema;

public class ClassDefinition
{
    public string Name { get; set; }

    public FieldDefinition[] Properties { get; set; }
}