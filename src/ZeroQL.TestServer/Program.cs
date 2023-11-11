using System.Buffers;
using System.Net;
using System.Text;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types.NodaTime;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ZeroQL.TestServer.Query;
using ZeroQL.TestServer.Query.Models;
using ObjectResult = HotChocolate.Execution.Processing.ObjectResult;
using UuidType = ZeroQL.TestServer.Query.Models.UuidType;

namespace ZeroQL.TestServer;

public class Program
{
    public const string TestServerUrlTemplate = "http://localhost:{0}/graphql";

    public static async Task Main(string[] args)
    {
        await StartServer(new ServerContext
        {
            Arguments = args,
            Port = 10_000,
        });
    }

    public static async Task StartServer(ServerContext context)
    {
        var builder = WebApplication.CreateBuilder(context.Arguments);
        var app = CreateApp(context, builder);

        await app.RunAsync(context.CancellationTokenSource.Token);
    }

    public static WebApplication CreateApp(ServerContext context, WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(context.Port));

        builder.Services.AddMemoryCache()
            .AddSha256DocumentHashProvider(HashFormat.Hex);

        var graphQLServer = builder.Services.AddGraphQLServer()
            .AddQueryType<Query.Query>()
            .AddMutationType<Mutation>()
            .AddType<UploadType>()
            .AddType<InstantType>()
            .BindRuntimeType<Uuid, UuidType>()
            .AddType<ZonedDateTimeType>()
            .AddType<IInterfaceThatNeverGetsUsed>()
            .AddType<Person>()
            .AddTypeExtension<LongOperationsExtensions>()
            .AddTypeExtension<UnionExtensions>()
            .AddTypeExtension<InterfacesExtensions>()
            .AddTypeExtension<NodeTimeGraphQLExtensions>()
            .AddTypeExtension<NodeTimeGraphQLMutations>()
            .AddTypeExtension<UserGraphQLExtensions>()
            .AddTypeExtension<DateMutation>()
            .AddTypeExtension<UserGraphQLMutations>()
            .AddTypeExtension<RoleGraphQLExtension>()
            .AddTypeExtension<JSONQueryExtensions>()
            .AddTypeExtension<CSharpKeywordsQueryExtensions>()
            .AddTypeExtension<CustomScalarsMutations>();


        builder.Services.Replace(
            ServiceDescriptor.Singleton<IHttpResponseFormatter, MessagePackHttpResponseFormatter>());

        if (string.IsNullOrEmpty(context.QueriesPath))
        {
            graphQLServer
                .UseAutomaticPersistedQueryPipeline()
                .AddInMemoryQueryStorage();
        }
        else
        {
            graphQLServer
                .UsePersistedQueryPipeline()
                .AddFileSystemQueryStorage(context.QueriesPath);
        }

        var app = builder.Build();

        app.MapGet("startup", () => Results.Ok());
        app.MapGraphQL();

        return app;
    }

    public static async Task<bool> VerifyServiceIsRunning(ServerContext context)
    {
        var httpClient = new HttpClient();
        for (var i = 0; i < 5; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(string.Format(TestServerUrlTemplate, context.Port) + "?sdl");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch
            {
            }

            await Task.Delay(500);
        }

        return false;
    }

    public static void StopServer(ServerContext context)
    {
        context.CancellationTokenSource.Cancel();
    }

    public class ServerContext
    {
        public int Port { get; set; } = 10_000;

        public string[] Arguments { get; set; } = Array.Empty<string>();

        public CancellationTokenSource CancellationTokenSource { get; set; } = new();

        public string? QueriesPath { get; set; }
    }
}

public class MessagePackHttpResponseFormatter : IHttpResponseFormatter
{
    static MessagePackSerializerOptions _options = MessagePackSerializerOptions.Standard.WithResolver(
        CompositeResolver.Create(
            new IMessagePackFormatter[] { new StringInterningFormatter() },
            new IFormatterResolver[]
            {
                new HotChocolateResolver(),
                StandardResolver.Instance,
            }));

    private readonly DefaultHttpResponseFormatter defaultFormatter;

    public GraphQLRequestFlags CreateRequestFlags(AcceptMediaType[] acceptMediaTypes)
    {
        return GraphQLRequestFlags.AllowAll;
    }

    public MessagePackHttpResponseFormatter()
    {
        this.defaultFormatter = new DefaultHttpResponseFormatter();
    }

    public async ValueTask FormatAsync(HttpResponse response,
        IExecutionResult result,
        AcceptMediaType[] acceptMediaTypes,
        HttpStatusCode? proposedStatusCode,
        CancellationToken cancellationToken)
    {
        var contentType = acceptMediaTypes.First();
        if (contentType.SubType == "json")
        {
            await defaultFormatter.FormatAsync(
                response,
                result,
                acceptMediaTypes,
                proposedStatusCode,
                cancellationToken);
            return;
        }

        if (contentType.SubType == "msgpack")
        {
            response.ContentType = "application/msgpack";
            if (result is QueryResult qr)
            {
                var bytes = MessagePackSerializer.Serialize(qr, _options);
                // var json = MessagePackSerializer.ConvertToJson(bytes);
                // var jsonBytes = Encoding.UTF8.GetBytes(json);
                await response.Body.WriteAsync(bytes, cancellationToken);
                return;
            }
        }

        throw new InvalidOperationException();
    }
}

public class HotChocolateResolver : IFormatterResolver
{
    public IMessagePackFormatter<T>? GetFormatter<T>()
    {
        if (typeof(T) == typeof(ListResult))
        {
            return (IMessagePackFormatter<T>)new ListResultMessagePackFormatter();
        }

        if (typeof(T) == typeof(ObjectResult))
        {
            return (IMessagePackFormatter<T>)new ObjectResultMessagePackFormatter();
        }

        if (typeof(T) == typeof(QueryResult))
        {
            return (IMessagePackFormatter<T>)new QueryResultMessagePackFormatter();
        }

        return null;
    }
}

public class QueryResultMessagePackFormatter : IMessagePackFormatter<QueryResult>
{
    public void Serialize(ref MessagePackWriter writer, QueryResult value, MessagePackSerializerOptions options)
    {
        var size = 0;
        if (value.Data is not null)
        {
            size++;
        }

        if (value.Errors is not null)
        {
            size++;
        }

        if (value.Extensions is not null)
        {
            size++;
        }

        writer.WriteMapHeader(size);
        if (value.Data is not null)
        {
            writer.Write("data");
            var formatter = options.Resolver.GetFormatter<object>();
            formatter?.Serialize(ref writer, value.Data, options);
        }

        if (value.Errors is not null)
        {
            writer.Write("errors");
            var formatter = options.Resolver.GetFormatter<object>();
            formatter?.Serialize(ref writer, value.Errors, options);
        }

        if (value.Extensions is not null)
        {
            writer.Write("extensions");
            var formatter = options.Resolver.GetFormatter<object>();
            formatter?.Serialize(ref writer, value.Extensions, options);
        }
    }

    public QueryResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class ListResultMessagePackFormatter : IMessagePackFormatter<ListResult>
{
    public ListResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public void Serialize(ref MessagePackWriter writer, ListResult value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(value.Count);
        foreach (var item in value)
        {
            var formatter = options.Resolver.GetFormatter<object>();
            formatter?.Serialize(ref writer, item, options);
        }
    }
}

public class ObjectResultMessagePackFormatter : IMessagePackFormatter<ObjectResult>
{
    public ObjectResult Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public void Serialize(ref MessagePackWriter writer, ObjectResult value, MessagePackSerializerOptions options)
    {
        var enumerable = value as IEnumerable<KeyValuePair<string, object>>;
        var count = enumerable?.Count() ?? 0;
        writer.WriteMapHeader(count);
        foreach (var field in value)
        {
            writer.Write(field.Name);
            var formatter = options.Resolver.GetFormatter<object>();
            formatter?.Serialize(ref writer, field.Value, options);
        }
    }
}