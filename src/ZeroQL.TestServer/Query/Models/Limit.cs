namespace ZeroQL.TestServer.Query.Models;

[GraphQLName("Limit")]
public class Limit_1
{
    public int Limit { get; set; }
    public int LimitInput { get; set; }
}

[GraphQLName("Limit2")]
public class Limit_2
{
    public int Value { get; set; }
    
    public Limit_2 Limit2 { get; set; }
}

[GraphQLName("Limit3")]
public class Limit_3
{
    public int Limit3Input { get; set; }
}

[GraphQLName("lower_case_type_name")]
public class LowerCaseTypeName
{
    public int Value { get; set; }
}