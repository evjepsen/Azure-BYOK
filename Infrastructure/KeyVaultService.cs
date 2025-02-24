using System.Text.Json;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Infrastructure.Interfaces;

namespace Infrastructure;

public class KeyVaultService : IKeyVaultService
{
    private KeyClient _client;

    public KeyVaultService()
    {
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


    public Response<KeyVaultKey> ImportKey(string name, string byokJson)
    {
        // Deserialize the BYOK JSON Web Key
        JsonWebKey? jwk;
        
        try
        {
            jwk = JsonSerializer.Deserialize<JsonWebKey>(byokJson);
            if (jwk == null) throw new JsonException("Deserialized JSON Web Key is null.");
        } 
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to deserialize BYOK JSON Web Key.", ex);
        }

        // Create key import options
        ImportKeyOptions importKeyOptions = new ImportKeyOptions(name, jwk)
        {
            HardwareProtected = true,
            Properties =
            {
                Enabled = true,
                Exportable = false,
                ExpiresOn = DateTimeOffset.Now.AddDays(90),
            }
        };
        
        // Import the key
        return _client.ImportKey(importKeyOptions);
    }

    public Response<KeyVaultKey> GenerateKek(string name)
    {
        var keyOptions = new CreateKeyOptions
        {
            Enabled = true,                                 // The key is ready to be used
            Exportable = false,                             // The private key cannot be exported 
            ExpiresOn = DateTimeOffset.Now.AddHours(12),    // Is active for 12 hours
            KeyOperations = { KeyOperation.UnwrapKey }      // Can only be used to unwrap the actual TDE protector
        };

        return _client.CreateKey(name, KeyType.RsaHsm, keyOptions);
    }
}