using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Infrastructure.Models;

namespace Infrastructure;

public class KeyVaultService : IKeyVaultService
{
    private readonly KeyClient _client;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TokenCredential _tokenCredential;
    private readonly string[] _scopes;
    private readonly IConfiguration _configuration; 

    public KeyVaultService(ITokenService tokenService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _tokenService = tokenService;
        _configuration = configuration;
        // Credentials for authentication
        _tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions{});
        
        // the azure key vault client
        _client = new KeyClient(
            new Uri(_configuration["VAULT_URI"]?? throw new InvalidOperationException("No Vault URI set")),
            _tokenCredential,
            new KeyClientOptions
            {
                Transport = new HttpClientTransport(_httpClientFactory.CreateClient("WaitAndRetry"))
            }
        );
        
        // Scope for Azure Key Vault and the credentials
        _scopes = ["https://vault.azure.net/.default"];
    }


    public async Task<KeyVaultUploadKeyResponse> UploadKey(string name, byte[] encryptedData, string kekId)
    {
        var httpClient = _httpClientFactory.CreateClient("WaitAndRetry");
        // Create the BYOK Blob for upload
        var transferBlob = _tokenService.CreateKeyTransferBlob(encryptedData, kekId);
        
        // (Manually) Set up the JsonWebKey
        var requestBody = _tokenService.CreateBodyForRequest(transferBlob);
        var requestBodyAsJson = TokenHelper.SerializeJsonObject(requestBody);
        
        string url = $"{_configuration["VAULT_URI"]}/keys/{name}/import?api-version=7.4";
        
        var content = new StringContent(requestBodyAsJson, Encoding.UTF8, "application/json");
        
        // Add the authentication token
        var authorizationToken = await _tokenCredential.GetTokenAsync(
            new TokenRequestContext(_scopes),
            default
        );
        
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authorizationToken.Token);
        
        // Send request
        var response = await httpClient.PostAsync(url, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to import key: {response.StatusCode} - {responseContent}");
        }
        
        // Deserialize into an object
        var responseObject = TokenHelper.DeserializeJsonObject<KeyVaultUploadKeyResponse>(responseContent);

        if (responseObject is null)
        {
            throw new JsonException("Could not deserialize the response from Azure");
        }

        return responseObject;
    }

    // Use RSA-HSM as Key Encryption Key
    public async Task<KeyVaultKey> GenerateKekAsync(string name)
    {
        var keyOptions = new CreateRsaKeyOptions(name, true)
        {
            Enabled = true,                                                         // The key is ready to be used
            Exportable = false,                                                     // The private key cannot be exported 
            ExpiresOn = DateTimeOffset.Now.AddHours(12),                            // Is active for 12 hours
            KeyOperations = { KeyOperation.Import },                                // Can only be used to import the TDE Protector
            KeySize = 2048                                                          // Key size of 2048 bits 
        };

        var kek = await _client.CreateRsaKeyAsync(keyOptions);

        if (!kek.HasValue)
        {
            throw new HttpRequestException("Failed to create key encryption key");
        }

        return kek.Value;
    }
    
    public async Task<PublicKeyKekPem> DownloadPublicKekAsPemAsync(string kekId)
    {
        // Get the key associated with the KEK ID
        var res = await _client.GetKeyAsync(kekId);

        if (!res.HasValue)
        {
            throw new HttpRequestException("Failed to get the key");
        }
        
        var key = res.Value.Key;
        var pem = key.ToRSA().ExportRSAPublicKeyPem();
        return new PublicKeyKekPem{PemString = pem};
    }
}