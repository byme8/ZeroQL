namespace ZeroQL.TestServer.Query;

[QueryType]
public class CSharpKeywordsQueryExtensions
{
    public int GetInt(int @int) => @int;

    public int GetObject(int @object) => @object;
}