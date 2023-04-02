using System.Collections.Generic;

namespace ZeroQL.Schema;

public record DirectiveDefinition(
    string Name,
    IReadOnlyDictionary<string, string?>? Arguments);