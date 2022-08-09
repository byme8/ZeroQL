namespace ZeroQL.Core.Schema;

public static class TypeDefinitionExtension
{
    public static string NameWithNullableAnnotation(this TypeDefinition type)
    {
        return $"{type.Name}{(type.CanBeNull ? "?" : "")}";
    }

    public static string NullableAnnotation(this TypeDefinition type)
    {
        return type.CanBeNull ? "?" : "";
    }
}

public abstract record TypeDefinition
{
    protected TypeDefinition(string name, bool canBeNull)
    {
        Name = name;
        CanBeNull = canBeNull;
    }

    public string Name { get; init; }

    public bool CanBeNull { get; init; }
}

public record ScalarTypeDefinition : TypeDefinition
{
    public ScalarTypeDefinition(string name) 
        : base(name, false)
    {
    }
}

public record EnumTypeDefinition : TypeDefinition
{
    public EnumTypeDefinition(string name) 
        : base(name, false)
    {
    }
}

public record ObjectTypeDefinition : TypeDefinition
{
    public ObjectTypeDefinition(string name) : base(name, false)
    {
    }
}

public record ListTypeDefinition : TypeDefinition
{
    public ListTypeDefinition(string name, bool canBeNull, TypeDefinition elementTypeDefinition) : base(name, canBeNull)
    {
        ElementTypeDefinition = elementTypeDefinition;
    }

    public TypeDefinition ElementTypeDefinition { get; init; }
}