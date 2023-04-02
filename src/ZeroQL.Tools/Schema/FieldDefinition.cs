namespace ZeroQL.Schema;

public record FieldDefinition(
    string Name,
    string GraphQLName,
    TypeDefinition TypeDefinition,
    ArgumentDefinition[] Arguments,
    DirectiveDefinition[]? Directives,
    string? DefaultValue = null);