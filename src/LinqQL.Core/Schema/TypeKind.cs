namespace LinqQL.Core.Schema;

public class TypeKind
{
    public static readonly Scalar Scalar = new();
    public static readonly Complex Complex = new();
    public static readonly Enum Enum = new();

}

public class Scalar : TypeKind
{
}
    
public class Enum : TypeKind
{
}

public class Complex : TypeKind
{
}

public class List : TypeKind
{
    public List(TypeKind element)
    {
        ElementTypeKind = element;
    }

    public TypeKind ElementTypeKind { get; }
}