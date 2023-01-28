using System.Collections.Generic;

namespace ZeroQL.Schema;

public record Definition(string Name);

public record ClassDefinition(string Name, FieldDefinition[] Properties, List<string> Implements)
    : Definition(Name);

public record InterfaceDefinition(string Name, FieldDefinition[] Properties)
    : Definition(Name);

public record UnionDefinition(string Name, string[] Types)
    : Definition(Name);

public record EnumDefinition(string Name, string[]? Values)
    : Definition(Name);

public record ScalarDefinition(string Name)
    : Definition(Name);