namespace ZeroQL.Schema;

public record ClassDefinition(string Name, FieldDefinition[] Properties, string[]? Implements);

public record InterfaceDefinition(string Name, FieldDefinition[] Properties);
