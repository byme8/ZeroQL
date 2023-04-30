namespace ZeroQL.TestServer.Query.Models;

public enum UserKind
{
    SupperGood,
    Good,
    Bad
}

public enum UserKindPascal
{
    [GraphQLName("SupperGood")]
    SupperGood,
    [GraphQLName("Good")]
    Good,
    [GraphQLName("Bad")]
    Bad
}