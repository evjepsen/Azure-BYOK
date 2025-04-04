using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public class KeyVaultService : IKeyVaultService
{
    private readonly KeyClient _client;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TokenCredential _tokenCredential;
    private readonly string[] _scopes;
    private readonly ApplicationOptions _applicationOptions;
    private readonly ILogger<KeyVaultService> _logger;

    public KeyVaultService(ITokenService tokenService, 
        IHttpClientFactory httpClientFactory, 
        IOptions<ApplicationOptions> applicationOptions,
        ILoggerFactory loggerFactory
    )
    {
        _logger = loggerFactory.CreateLogger<KeyVaultService>();
        _httpClientFactory = httpClientFactory;
        _tokenService = tokenService;
        _applicationOptions = applicationOptions.Value;
        // Credentials for authentication
        _tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions());
        
        // the azure key vault client
        _client = new KeyClient(
            new Uri(_applicationOptions.VaultUri),
            _tokenCredential,
            new KeyClientOptions
            {
                Transport = new HttpClientTransport(_httpClientFactory.CreateClient("WaitAndRetry"))
            }
        );
        
        // Scope for Azure Key Vault and the credentials
        _scopes = ["https://vault.azure.net/.default"];
    }


    public async Task<KeyVaultUploadKeyResponse> UploadKey(string name, ITransferBlobStrategy transferBlobStrategy, string[] keyOperations)
    {
        var httpClient = _httpClientFactory.CreateClient("WaitAndRetry");
        
        var transferBlob = transferBlobStrategy.GenerateTransferBlob();
        
        // (Manually) Set up the JsonWebKey
        var requestBody = _tokenService.CreateBodyForRequest(transferBlob, keyOperations);
        var requestBodyAsJson = TokenHelper.SerializeJsonObject(requestBody);
        
        var url = $"{_applicationOptions.VaultUri}/keys/{name}/import?api-version=7.4";
        
        var content = new StringContent(requestBodyAsJson, Encoding.UTF8, "application/json");
        
        // Add the authentication token
        var authorizationToken = await _tokenCredential.GetTokenAsync(
            new TokenRequestContext(_scopes),
            CancellationToken.None
        );
        
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authorizationToken.Token);
        
        // Send request
        _logger.LogInformation("Uploading key to Azure Key Vault");
        var response = await httpClient.PostAsync(url, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to import key: {statusCode} - {responseContent}", response.StatusCode, responseContent);
            throw new HttpRequestException($"Failed to import key: {response.StatusCode} - {responseContent}");
        }
        
        // Deserialize into an object
        var responseObject = TokenHelper.DeserializeJsonObject<KeyVaultUploadKeyResponse>(responseContent);

        if (responseObject is null)
        {
            _logger.LogError("Failed to deserialize the response from Azure");
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

        _logger.LogInformation("Creating RSA key encryption key");
        var kek = await _client.CreateRsaKeyAsync(keyOptions);

        if (!kek.HasValue)
        {
            _logger.LogError("Failed to create key encryption key");
            throw new HttpRequestException("Failed to create key encryption key");
        }

        return kek.Value;
    }
    
    public async Task<PublicKeyKekPem> DownloadPublicKekAsPemAsync(string kekName)
    {
        // Get the key associated with the KEK ID
        _logger.LogInformation("Requesting the key encryption key with ID: {kekId}", kekName);
        var res = await _client.GetKeyAsync(kekName);

        if (!res.HasValue)
        {
            _logger.LogError("Failed to get the key encryption key with ID: {kekId}", kekName);
            throw new HttpRequestException("Failed to get the key");
        }
        
        var keyVaultKey = res.Value;
        var pem = keyVaultKey.Key.ToRSA().ExportRSAPublicKeyPem();
        
        _logger.LogInformation("Returning the public key as PEM string");
        return new PublicKeyKekPem
        {
            KekId = keyVaultKey.Id,
            PemString = pem
        };
    }
    
    public async Task<DeletedKey> DeleteKeyAsync(string keyId)
    {
        _logger.LogInformation("Deleting the key with ID: {keyId}", keyId);
        var deleteKeyOperationAsync = await _client.StartDeleteKeyAsync(keyId);
        var res = await deleteKeyOperationAsync.WaitForCompletionAsync();
        
        if (!res.HasValue)
        {
            _logger.LogError("Failed to delete the key with ID: {keyId}", keyId);
            throw new HttpRequestException("Failed to delete the key");
        }
        
        return res.Value;
    }
    
    public async Task<Response> PurgeDeletedKeyAsync(string keyId)
    {
        _logger.LogInformation("Purging the key with ID: {keyId}", keyId);
        var purgeOperation = await _client.PurgeDeletedKeyAsync(keyId);
        
        return purgeOperation;
    }

    public async Task<RecoverDeletedKeyOperation> RecoverDeletedKeyAsync(string keyName)
    {
        _logger.LogInformation("Recovering the key with name: {keyName}", keyName);
        var recoverOperation = await _client.StartRecoverDeletedKeyAsync(keyName);
        await recoverOperation.WaitForCompletionAsync();
        
        if (!recoverOperation.HasCompleted)
        {
            _logger.LogError("Failed to recover the key {keyName}", keyName);
            throw new HttpRequestException($"Failed to recover the key {keyName}");
        }
        
        return recoverOperation;
    }

    public async Task<bool> CheckIfKeyExistsAsync(string keyName)
    {
        bool doesKeyExist;
        try
        {
            var response = await _client.GetKeyAsync(keyName);
            doesKeyExist = response.HasValue;
        }
        catch (RequestFailedException ex)
        {
            if (ex.Status == 404) doesKeyExist = false;
            else throw;
        }
        
        return doesKeyExist;
    }
}