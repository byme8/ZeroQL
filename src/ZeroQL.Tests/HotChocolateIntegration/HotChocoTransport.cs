using HotChocolate;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using System.Text.Json;
using ZeroQL.Json;
using GraphQLRequest = ZeroQL.Internal.GraphQLRequest;

namespace ZeroQL.Tests.HotChocolateIntegration;

public class HotChocoTransport : IGraphQLTransport
{
    private IRequestExecutor requestExecutor;

    public HotChocoTransport(IRequestExecutor requestExecutor)
    {
        this.requestExecutor = requestExecutor ?? throw new ArgumentNullException(nameof(requestExecutor));
    }

    public IGraphQLTransportContent CreateContent(GraphQLRequest queryRequest) =>
        new HotChocoTransportContent(queryRequest.Query);

    public async Task<GraphQLResponse<TQuery>> DeliverAsync<TQuery>(string query, IGraphQLTransportContent transportContent)
    {
        if (!(transportContent is HotChocoTransportContent hotChocoTransportContent))
            throw new ArgumentException($"{nameof(transportContent)} was not of type {typeof(HotChocoTransportContent)}.  Type={transportContent.GetType()}", nameof(transportContent));

        IExecutionResult executionResult = await requestExecutor.ExecuteAsync(hotChocoTransportContent.GraphQLRequest);

        var errors = GetGraphQueryErrors(executionResult.Errors);
        var extensions = executionResult.Extensions is null ? new Dictionary<string, object>() : new Dictionary<string, object>(executionResult.Extensions);
        if (executionResult.Errors?.Any() ?? false)
        {
            return new GraphQLResponse<TQuery>() { Query = query, Errors = errors, Extensions = extensions };
        }

        var httpResultSerializer = new DefaultHttpResultSerializer();

        //var responseJson = string.Empty;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            await httpResultSerializer.SerializeAsync(executionResult, memoryStream, cancellationTokenSource.Token);

            memoryStream.Position = 0;

            var response = await JsonSerializer.DeserializeAsync<GraphQLResponse<TQuery>>(memoryStream, ZeroQLJsonOptions.Options);
            return response;
        }
    }

    private GraphQueryError[] GetGraphQueryErrors(IReadOnlyList<IError> errors)
    {
        if (errors is null)
            return null;

        List<GraphQueryError> results = new();
        foreach (var error in errors)
        {
            var graphQueryError = new GraphQueryError()
            {
                Message = error.Message
            };

            if (error.Path is not null)
            {
                graphQueryError.Path = new object[] { error.Path.Print() };
            }

            if (error.Extensions is not null)
            {
                graphQueryError.Extensions = new Dictionary<string, object>(error.Extensions);
            }

            results.Add(graphQueryError);
        }

        return results.ToArray();
    }

}
