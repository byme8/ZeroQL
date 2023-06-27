using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ZeroQL.Internal;
using ZeroQL.Schema;

namespace ZeroQL.Bootstrap.Generators;

public static class DirectiveGenerator
{
    public static MemberDeclarationSyntax CopyDirectives(this MemberDeclarationSyntax selector, FieldDefinition field)
    {
        var possibleDeprecatedDirective = field.Directives?.FirstOrDefault(o => o.Name == "deprecated");
        if (possibleDeprecatedDirective is { } directive)
        {
            var reason = directive.Arguments?.GetValueOrDefault("reason");
            if (reason is not null)
            {
                selector = selector.AddAttributeWithStringParameter(ZeroQLGenerationInfo.DeprecatedAttribute, reason);
            }
            else
            {
                selector = selector.AddAttributeWithStringParameter(ZeroQLGenerationInfo.DeprecatedAttribute);
            }
        }

        return selector;
    }
}