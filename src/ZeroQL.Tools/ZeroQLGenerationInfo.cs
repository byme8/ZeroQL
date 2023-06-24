namespace ZeroQL;

public class ZeroQLGenerationInfo
{
    public static string Version { get; } = typeof(ZeroQLGenerationInfo).Assembly.GetName().Version!.ToString();
    
    public static string CodeGenerationAttribute { get; } = $@"System.CodeDom.Compiler.GeneratedCode(""ZeroQL"", ""{Version}"")";
    
    public static string GraphQLNameAttribute => "ZeroQL.GraphQLName";

    public static string GraphQLTypeAttribute => "ZeroQL.GraphQLType";
    
    public static string JsonPropertyNameAttribute => "JsonPropertyName";

    public static string JsonIgnoreAttribute => "JsonIgnore";

    public static string DeprecatedAttribute => "System.ObsoleteAttribute";
}