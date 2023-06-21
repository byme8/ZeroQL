namespace ZeroQL.TestServer.Query.Models;

[GraphQLName("Limit")]
public class Limit_1
{
    public int Limit { get; set; }
}

[GraphQLName("Limit2")]
public class Limit_2
{
    public int Value { get; set; }
    
    public Limit_2 Limit2 { get; set; }
}