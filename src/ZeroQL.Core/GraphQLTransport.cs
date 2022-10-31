using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ZeroQL.Internal;
using ZeroQL.Json;

namespace ZeroQL;


public interface IGraphQLTransport
{
	public IGraphQLTransportContent CreateContent(GraphQLRequest queryRequest);
	public Task<GraphQLResponse<TQuery>> DeliverAsync<TQuery>(string query, IGraphQLTransportContent transportContent);
}

public class HttpTransport : IGraphQLTransport, IDisposable
{
	private HttpClient httpClient;

    public HttpTransport(HttpClient httpClient)
	{
		this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

    public IGraphQLTransportContent CreateContent(GraphQLRequest queryRequest)
	{
		throw new Exception($"This method shouldn't get called.  Currently the source code to {nameof(CreateContent)} is dynamically generated and therefore isn't on this {nameof(HttpTransport)} class");
	}


    public async Task<GraphQLResponse<TQuery>> DeliverAsync<TQuery>(string query, IGraphQLTransportContent transportContent)
	{
		if (!(transportContent is HttpTransportContent httpTransportContent))
			throw new ArgumentException($"{nameof(transportContent)} was not of type {typeof(HttpTransportContent)}.  Type={transportContent.GetType()}", nameof(transportContent));

        var response = await httpClient.PostAsync("", httpTransportContent.HttpContent);
        var responseJson = await response.Content.ReadAsStreamAsync();
        var qlResponse = await JsonSerializer.DeserializeAsync<GraphQLResponse<TQuery>>(responseJson, ZeroQLJsonOptions.Options);

		return qlResponse;
    }


    public void Dispose()
	{
		httpClient.Dispose();
	}
}
