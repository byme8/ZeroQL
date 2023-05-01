namespace ZeroQL.TestServer.Query.Models;

public enum UserKind
{
    SupperGood,
    Good,
    Bad
}

public enum UserKindPascal
{
    [GraphQLName("default")]
    Default,
    [GraphQLName("SupperGood")]
    SupperGood,
    [GraphQLName("Good")]
    Good,
    [GraphQLName("Bad")]
    Bad
}