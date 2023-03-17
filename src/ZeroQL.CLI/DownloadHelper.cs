using System.Net.Http.Headers;
using HotChocolate.Utilities.Introspection;
using ZeroQL.CLI.Commands;

namespace ZeroQL.CLI;

public static class DownloadHelper
{
    public static async Task<string> DownloadSchema(this Uri schemaUri, bool force, string? accessToken, string? authScheme,
        IEnumerable<string>? customHeaders)
    {
        const string sdlFilename = "schema.graphql";

        if (!force && File.Exists(sdlFilename))
        {
            return sdlFilename;
        }

        var client = CreateHttpClient(schemaUri, accessToken, authScheme, ParseHeadersArgument(customHeaders));
        await using var stream = File.OpenWrite(sdlFilename);
        await IntrospectionClient.Default.DownloadSchemaAsync(client, stream,
            new CancellationTokenSource(client.Timeout).Token);

        return sdlFilename;
    }

    private static Dictionary<string, IEnumerable<string>> ParseHeadersArgument(
        IEnumerable<string?>? arguments)
    {
        var headers = new Dictionary<string, IEnumerable<string>>();

        if (arguments == null)
        {
            return headers;
        }

        foreach (var argument in arguments)
        {
            var argumentParts = argument?.Trim().Split("=", 2);
            if (argumentParts?.Length != 2)
            {
                continue;
            }

            var argumentKey = argumentParts[0];

            var argumentValueParts = argumentParts[1].Trim().Split(",");

            _ = headers.TryAdd(argumentKey, argumentValueParts);
        }

        return headers;
    }

    private static HttpClient CreateHttpClient(
        Uri uri,
        string? token,
        string? scheme,
        Dictionary<string, IEnumerable<string>>? customHeaders)
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15),
            BaseAddress = uri
        };

        httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(
                new ProductHeaderValue(
                    "ZeroQL",
                    typeof(GenerateCommand).Assembly.GetName().Version!.ToString())));

        if (token is not null)
        {
            if (string.IsNullOrWhiteSpace(scheme))
            {
                httpClient.DefaultRequestHeaders
                    .TryAddWithoutValidation("Authorization", token);
            }
            else
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(scheme, token);
            }
        }

        if (customHeaders is null)
        {
            return httpClient;
        }

        foreach (var headerKey in customHeaders.Keys)
        {
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                headerKey,
                customHeaders[headerKey]);
        }

        return httpClient;
    }
}