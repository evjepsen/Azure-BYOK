using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Test.TestHelpers;

public class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public FakeHttpClientFactory()
    {
        _retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600));
    }

    public HttpClient CreateClient(string name)
    {
        var policyHandler = new PolicyHttpMessageHandler(_retryPolicy)
        {
            InnerHandler = new HttpClientHandler()
        };

        return new HttpClient(policyHandler);    
    }
}