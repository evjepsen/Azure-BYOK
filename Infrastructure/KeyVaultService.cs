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
            new Uri("https://kv-byok.vault.azure.net/"),
            credentials);
    }
    
    public Response<KeyVaultKey> ImportKey(string name, string value, KeyType keyType)
    {
        // TODO! - Check that the key type is "hsm"

        var keyImportOptions = new ImportKeyOptions(name, new JsonWebKey
        {
            KeyType = keyType, // Ensures HSM-backed key
            KeyOps = {  },
            
        });

        return _client.ImportKey(keyImportOptions);
    }
}