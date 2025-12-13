using System;
using GraphQL.TestServer;

namespace ZeroQL.TestApp;

public static partial class QueryFragments
{
    [GraphQLFragment]
    public static partial UserModel ExposedFragmentUserWithRole(this User user)
    {
        return new UserModel(user.FirstName, user.LastName, user.Role(o => o.Name));
    }

    [GraphQLFragment]
    public static UserModel AsUserWithRoleNameBody(this User user)
    {
        return new UserModel(user.FirstName, user.LastName, user.Role(o => o.Name));
    }

    [GraphQLFragment]
    public static UserModel AsUserWithRoleNameInitializers(this User user)
    {
        return new UserModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role(o => o.Name)
        };
    }
    
    [GraphQLFragment]
    public static UserRead AsUserRead(this User user) 
        => new UserRead()
        {
            Id = user.Id,
            Kind2 = (UserKind2)user.UserKind
        };

    [GraphQLFragment]
    public static UserModel AsUserWithRoleNameExpression(this User user)
        => new UserModel(user.FirstName, user.LastName, user.Role(o => o.Name));

    [GraphQLFragment]
    public static UserModel? GetUserById(this Query query, int id)
        => query.User(id, o => o!.AsUserWithRoleNameBody());

    [GraphQLFragment]
    public static int SetValues(this Mutation mutation,
        string text,
        byte value1,
        byte? value2,
        short value3,
        short? value4,
        int value5,
        int? value6,
        long value7,
        long? value8,
        float value9,
        float? value10,
        double value11,
        double? value12,
        decimal value13,
        decimal? value14,
        DateTime value15,
        DateTime? value16,
        DateOnly value17,
        DateOnly? value18,
        Guid value19,
        Guid? value20,
        Guid[] value21,
        Guid[]? value22,
        Guid[] value23,
        Guid[]? value24,
        Guid[] value25,
        Guid[]? value26,
        KeyValuePairOfStringAndStringInput[] value27,
        KeyValuePairOfStringAndStringInput[]? value28,
        KeyValuePairOfStringAndStringInput value29,
        KeyValuePairOfStringAndStringInput? value30,
        DateTimeOffset value31,
        DateTimeOffset? value32
    )
        => mutation.AddValues(text,
            value1,
            value2,
            value3,
            value4,
            value5,
            value6,
            value7,
            value8,
            value9,
            value10,
            value11,
            value12,
            value13,
            value14,
            value15,
            value16,
            value17,
            value18,
            value19,
            value20,
            value21,
            value22,
            value23,
            value24,
            value25,
            value26,
            value27,
            value28,
            value29,
            value30,
            value31,
            value32);

}