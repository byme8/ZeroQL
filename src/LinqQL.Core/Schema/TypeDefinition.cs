namespace LinqQL.Core.Schema;

public class ClassDefinition
{
    public string Name { get; set; }

    public FieldDefinition[] Properties { get; set; }
}

public class TypeDefinition
{
    public string Name { get; set; }

    public TypeKind TypeKind { get; set; }
}

public enum TypeKind
{
    Scalar,
    Enum,
    Object,
    Array
}

public class FieldDefinition
{
    public string Name { get; set; }

    public string TypeName { get; set; }

    public ArgumentDefinition[] Arguments { get; set; }

    public TypeKind TypeKind { get; set; }
}

public class ArgumentDefinition
{
    public string Name { get; set; }

    public string TypeName { get; set; }
}