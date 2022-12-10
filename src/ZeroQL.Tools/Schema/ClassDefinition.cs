using System.Collections.Generic;

namespace ZeroQL.Schema;

public record ClassDefinition(string Name, FieldDefinition[] Properties, List<string> Implements);

public record InterfaceDefinition(string Name, FieldDefinition[] Properties);

public record UnionDefinition(string Name, string[] Types);
