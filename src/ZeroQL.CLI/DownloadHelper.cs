using System.Net.Http.Headers;
using HotChocolate.Language.Utilities;
using HotChocolate.Utilities.Introspection;
using ZeroQL.CLI.Commands;

namespace ZeroQL.CLI;

public static class DownloadHelper
{
    public static async Task<Result<Unit>> DownloadSchema(
        Uri schemaUri,
        string output,
        string? accessToken,
        string? authScheme,
        KeyValuePair<string, string>[]? customHeaders,
        int? timeout,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = CreateHttpClient(schemaUri, accessToken, authScheme, customHeaders, timeout);
            await using var stream = File.Create(output);
            var node = await IntrospectionClient.IntrospectServerAsync(client, cancellationToken);

            await node.PrintToAsync(stream, cancellationToken: cancellationToken);
            return Unit.Default;
        }
        catch (HttpRequestException ex)
        {
            return new Error(ex.Message);
        }
        catch (TaskCanceledException)
        {
            return new Error("Download schema timed out");
        }
        catch (Exception e)
        {
            throw;
        }
    }

    private static HttpClient CreateHttpClient(
        Uri uri,
        string? token,
        string? scheme,
        KeyValuePair<string, string>[]? customHeaders,
        int? timeout)
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeout ?? 30),
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