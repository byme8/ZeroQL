using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ZeroQL.TestApp.Services;

public class GraphQLClientMethodWrapper
{
    public static async Task<T?> MakeQuery<T, TQuery, TMutation>(
        GraphQLClient<TQuery, TMutation> client, 
        [GraphQLLambda]Func<TQuery, T> query,
        [CallerArgumentExpression(nameof(query))] string queryKey = "")
        where T : class
    {
        var result = await client.Query(query, queryKey: queryKey);
        
        if (result.Errors != null)
        {
            Console.WriteLine("Errors:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine(error.Message);
            }

            return null;
        }
        
        return result.Data;
    }
}