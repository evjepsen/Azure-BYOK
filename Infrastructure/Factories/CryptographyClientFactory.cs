using System.Collections.Concurrent;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using Infrastructure.Interfaces;

namespace Infrastructure.Factories;

public class CryptographyClientFactory : ICryptographyClientFactory
{
    private readonly ConcurrentDictionary<Uri, CryptographyClient> _clientCache = new();
    private readonly TokenCredential _tokenCredential;
    private readonly IHttpClientFactory _httpClientFactory;

    public CryptographyClientFactory(IHttpClientFactory httpClientFactory)
    {
        _tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions());
        _httpClientFactory = httpClientFactory;
    }

    public CryptographyClient GetCryptographyClient(Uri keyId)
    {
        return _clientCache.GetOrAdd(keyId, id => 
            new CryptographyClient(id,
                _tokenCredential,
                new CryptographyClientOptions
                {
                    Transport = new HttpClientTransport(_httpClientFactory.CreateClient("WaitAndRetry"))
                })
            );
    }
}