namespace ZeroQL.Schema;

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

public abstract record TypeDefinition(string Name, bool CanBeNull);

public record ScalarTypeDefinition(string Name) :
    TypeDefinition(Name, false);

public record EnumTypeDefinition(string Name)
    : TypeDefinition(Name, false);

public record ObjectTypeDefinition(string Name)
    : TypeDefinition(Name, false);

public record ListTypeDefinition(string Name, bool CanBeNull, TypeDefinition ElementTypeDefinition)
    : TypeDefinition(Name, CanBeNull);