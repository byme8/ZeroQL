using System;
using System.Net.Http;

namespace ZeroQL;

public interface IGraphQLTransportContent
{
}

public class HttpTransportContent : IGraphQLTransportContent
{
	public HttpTransportContent(HttpContent httpContent)
	{
		HttpContent = httpContent ?? throw new ArgumentNullException(nameof(httpContent));
	}

	public HttpContent HttpContent { get; }
}