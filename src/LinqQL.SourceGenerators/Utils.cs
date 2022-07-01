using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace LinqQL.SourceGenerators
{
    public static class Utils
    {
        public static string FirstToLower(this string text)
        {
            var first = text.Substring(0, 1);
            var tail = text.Substring(1);

            return first.ToLower() + tail;
        }

        public static string Join(this IEnumerable<string> values, string separator = ", ")
        {
            return string.Join(separator, values);
        }

        public static string Wrap(this string text, string left = "", string right = "")
        {
            return $"{left}{text}{right}";
        }

        public static string JoinWithNewLine(this IEnumerable<string> values, string separator = "")
        {
            return string.Join($"{separator}{Environment.NewLine}", values);
        }

        private static Dictionary<string, string> CSharpToGraphQL = new Dictionary<string, string>
        {
            { "Int32", "Int" },
            { "string", "String" }
        };

        public static string ToStringWithNullable(this ITypeSymbol typeSymbol)
        {
            return typeSymbol.NullableAnnotation switch
            {
                NullableAnnotation.None => Map(typeSymbol.Name) + "!",
                NullableAnnotation.NotAnnotated => Map(typeSymbol.Name) + "!",
                NullableAnnotation.Annotated => Map(typeSymbol.Name),
                _ => throw new ArgumentOutOfRangeException()
            };

            string Map(string name)
            {
                if (CSharpToGraphQL.ContainsKey(name))
                {
                    return CSharpToGraphQL[name];
                }

                return name;
            }
        }
    }
}