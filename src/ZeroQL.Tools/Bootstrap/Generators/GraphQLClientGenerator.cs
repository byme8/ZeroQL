using ZeroQL.Core.Enums;

namespace ZeroQL.Bootstrap.Generators;

public static class GraphQLClientGenerator
{
    public static string GenerateClient(
        this GraphQlGeneratorOptions options,
        string? queryType,
        string? mutationType)
    {
        var clientName = options.ClientName;
        var queryTypeName = queryType ?? "ZeroQL.Unit";
        var mutationTypeName = mutationType ?? "ZeroQL.Unit";

        var accessModifier = options.Visibility == ClientVisibility.Public
            ? "public"
            : "internal";

        var name = ClientName(clientName);
        var source = @$"
            {accessModifier} class {name} : global::ZeroQL.GraphQLClient<{queryTypeName}, {mutationTypeName}>
            {{
                private static IZeroQLSerializer DefaultSerializer {{ get; }} = {clientName}JsonInitializer.CreateSerializer();
        
                public {name}(
                    global::System.Net.Http.HttpClient client,  
                    IZeroQLSerializer? options = null,
                    PipelineType pipelineType = PipelineType.Full) 
                    : base(client, options ?? DefaultSerializer, pipelineType)
                {{
                }}

                public {name}(
                    global::ZeroQL.IHttpHandler client, 
                    IZeroQLSerializer? options = null,
                    PipelineType pipelineType = PipelineType.Full) 
                    : base(client, options ?? DefaultSerializer, pipelineType)
                {{
                }}
            }}";

        return source;
    }

    public static string ClientName(string? clientName) 
        => clientName ?? "GraphQLClient";
}