using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.TestServer;
using ZeroQL.Core;
using ZeroQL.TestApp.Models;

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
    public static UserModel AsUserWithRoleNameExpression(this User user)
        => new UserModel(user.FirstName, user.LastName, user.Role(o => o.Name));

    [GraphQLFragment]
    public static UserModel? GetUserById(this Query query, int id)
        => query.User(id, o => o!.AsUserWithRoleNameBody());

    [GraphQLFragment]
    public static int SetValues(this Mutation mutation,
        string Text,
        byte Value1,
        byte? Value2,
        short Value3,
        short? Value4,
        int Value5,
        int? Value6,
        long Value7,
        long? Value8,
        float Value9,
        float? Value10,
        double Value11,
        double? Value12,
        decimal Value13,
        decimal? Value14,
        DateTime Value15,
        DateTime? Value16,
        DateOnly Value17,
        DateOnly? Value18,
        Guid Value19,
        Guid? Value20,
        Guid[] Value21,
        Guid[]? Value22,
        Guid[] Value23,
        Guid[]? Value24,
        Guid[] Value25,
        Guid[]? Value26,
        KeyValuePairOfStringAndStringInput[] Value27,
        KeyValuePairOfStringAndStringInput[]? Value28,
        KeyValuePairOfStringAndStringInput Value29,
        KeyValuePairOfStringAndStringInput? Value30,
        DateTimeOffset Value31,
        DateTimeOffset? Value32
    )
        => mutation.AddValues(Text,
            Value1,
            Value2,
            Value3,
            Value4,
            Value5,
            Value6,
            Value7,
            Value8,
            Value9,
            Value10,
            Value11,
            Value12,
            Value13,
            Value14,
            Value15,
            Value16,
            Value17,
            Value18,
            Value19,
            Value20,
            Value21,
            Value22,
            Value23,
            Value24,
            Value25,
            Value26,
            Value27,
            Value28,
            Value29,
            Value30,
            Value31,
            Value32);

}