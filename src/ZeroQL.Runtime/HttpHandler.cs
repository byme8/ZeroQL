using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroQL;

public interface IHttpHandler : IDisposable
{
    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}

public class HttpHandler(HttpClient client, bool disposeClient = false) : IHttpHandler
{
    public void Dispose()
    {
        if (disposeClient)
        {
            client.Dispose();
        }
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return client.SendAsync(request, cancellationToken);
    }
}