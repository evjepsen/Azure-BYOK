using System.Text;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Infrastructure.Interfaces;

namespace Infrastructure;

public class KeyVaultService : IKeyVaultService
{
    private readonly KeyClient _client;
    private readonly ITokenService _tokenService;
    private readonly HttpClient _httpClient;

    public KeyVaultService(ITokenService tokenService)
    {
        _httpClient = new HttpClient();
        _tokenService = tokenService;
        // Credentials for authentication
        var credentials = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            // Exclude ManagedIdentityCredential when running locally
            ManagedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")
        });
        
        // the azure key vault client
        _client = new KeyClient(
            new Uri("https://byok-cloud-kv.vault.azure.net/"),
            credentials);
    }


    public async Task<string> ImportKey(string name, byte[] encryptedData, string kekId)
    {
        // Create the BYOK Blob for upload
        var transferBlob = _tokenService.CreateKeyTransferBlob(encryptedData, kekId);
        
        // (Manually) Set up the JsonWebKey
        var requestBody = _tokenService.CreateBodyForRequest(transferBlob);
        
        string url = $"https://byok-cloud-kv.vault.azure.net/keys/{name}/import?api-version=7.4";
        
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        
        // Send request
        var response = await _httpClient.PostAsync(url, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to import key: {response.StatusCode} - {responseContent}");
        }
        
        return responseContent;
    }

    // Use RSA-HSM as Key Encryption Key
    public Response<KeyVaultKey> GenerateKek(string name)
    {
        var keyOptions = new CreateRsaKeyOptions(name, true)
        {
            Enabled = true,                                                         // The key is ready to be used
            Exportable = false,                                                     // The private key cannot be exported 
            ExpiresOn = DateTimeOffset.Now.AddHours(12),                            // Is active for 12 hours
            KeyOperations = { KeyOperation.Import },                                // Can only be used to import the TDE Protector
            KeySize = 4096                                                          // Key size of 4096 bits 
        };

        return _client.CreateRsaKey(keyOptions);
    }
    
}