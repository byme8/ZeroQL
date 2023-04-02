namespace ZeroQL;

public class ZeroQLGenerationInfo
{
    public static string Version { get; } = typeof(ZeroQLGenerationInfo).Assembly.GetName().Version!.ToString();
    
    public static string CodeGenerationAttribute { get; } = $@"System.CodeDom.Compiler.GeneratedCode(""ZeroQL"", ""{Version}"")";
    
    public static string GraphQLFieldSelectorAttribute => "ZeroQL.GraphQLFieldSelector";

    public static string GraphQLJsonAttribute => "JsonPropertyName";

    public static string DeprecatedAttribute => "System.ObsoleteAttribute";
}