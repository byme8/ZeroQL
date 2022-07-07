namespace LinqQL.Core.Schema;

public static class TypeDefinitionExtension
{
    public static string NullableAnnotation(this TypeDefinition type)
    {
        return type.CanBeNull ? "?" : "";
    }
}

public abstract record TypeDefinition
{
    public string Name { get; set; }

    public bool CanBeNull { get; set; }
}

public record ScalarTypeDefinition : TypeDefinition
{
    public ScalarTypeDefinition(string name)
    {
        Name = name;
    }
}

public record EnumTypeDefinition : TypeDefinition
{
    public EnumTypeDefinition(string name)
    {
        Name = name;
    }
}

public record ObjectTypeDefinition : TypeDefinition
{
    public ObjectTypeDefinition(string name)
    {
        Name = name;
    }
}

public record ListTypeDefinition : TypeDefinition
{
    public TypeDefinition ElementTypeDefinition { get; set; }
}