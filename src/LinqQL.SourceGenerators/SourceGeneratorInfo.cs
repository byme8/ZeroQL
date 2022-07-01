namespace LinqQL.SourceGenerators
{
    public class SourceGeneratorInfo
    {
        public static string Version { get; } = typeof(SourceGeneratorInfo).Assembly.GetName().Version.ToString();
        public static string CodeGenerationAttribute { get; } = $@"[System.CodeDom.Compiler.GeneratedCode(""LinQL"", ""{Version}"")]";
    }
}