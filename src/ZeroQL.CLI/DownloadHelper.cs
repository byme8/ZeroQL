using System.Net.Http.Headers;
using HotChocolate.Language.Utilities;
using HotChocolate.Utilities.Introspection;
using ZeroQL.CLI.Commands;

namespace ZeroQL.CLI;

public static class DownloadHelper
{
    public static async Task DownloadSchema(
        Uri schemaUri,
        string output,
        string? accessToken,
        string? authScheme,
        KeyValuePair<string, string>[]? customHeaders,
        CancellationToken cancellationToken)
    {
        var client = CreateHttpClient(schemaUri, accessToken, authScheme, customHeaders);
        await using var stream = File.OpenWrite(output);
        var document = await IntrospectionClient.IntrospectServerAsync(client,  cancellationToken);
        await document.PrintToAsync(stream, cancellationToken: cancellationToken, indented: true);
    }

    private static HttpClient CreateHttpClient(
        Uri uri,
        string? token,
        string? scheme,
        KeyValuePair<string, string>[]? customHeaders)
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

        foreach (var header in customHeaders)
        {
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                header.Key,
                header.Value);
        }

        return httpClient;
    }
}