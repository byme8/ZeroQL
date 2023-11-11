using GraphQL.TestServer;
using ZeroQL.TestApp.Models;

namespace ZeroQL.TestApp;

public record GetUserById(int Id) : GraphQL<Query, UserModel?>
{
    public override UserModel? Execute(Query query)
    {
        return query.User(Id, o => new UserModel(o!.FirstName, o.LastName, o.Role(role => role!.Name)!));
    }
}

public record GetUserByIdWithLocalFunction(int Id) : GraphQL<Query, UserModel?>
{
    public override UserModel? Execute(Query query) => 
        query.User(Id, GetUserModel);

    static UserModel GetUserModel(User? o) => new(o!.FirstName, o.LastName, o.Role(role => role!.Name)!);
}

public record GetUserByIdExpression(int Id) : GraphQL<Query, UserModel?>
{
    public override UserModel? Execute(Query query)
        => query.User(Id, o => new UserModel(o!.FirstName, o.LastName, o.Role(role => role!.Name)!));
}

public record AddUser(string FirstName, string LastName) : GraphQL<Mutation, int>
{
    public override int Execute(Mutation query)
    {
        return query.AddUser(FirstName, LastName, o => o.Id);
    }
}

public record AddAvatar(int UserId, Upload File) : GraphQL<Mutation, int>
{
    public override int Execute(Mutation query)
    {
        return query.AddUserProfileImage(UserId, File);
    }
}