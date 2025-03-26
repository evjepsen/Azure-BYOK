using Azure.Core;
using Azure.Identity;
using Infrastructure.Interfaces;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest;

namespace Infrastructure;

public class KeyVaultManagementService : IKeyVaultManagementService
{
    private readonly IConfiguration _configuration;
    private readonly KeyVaultManagementClient _keyVaultManagementClient;
    
    // public KeyVaultService(ITokenService tokenService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    public KeyVaultManagementService(IConfiguration configuration)
    {
        _configuration = configuration;
        // Get management token.
        var tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions());
        var tokenRequestContext = new TokenRequestContext(["https://management.azure.com/.default"]);
        var accessToken = tokenCredential.GetToken(tokenRequestContext);
        // Create ServiceClientCredentials credentials.
        ServiceClientCredentials credentials = new TokenCredentials(accessToken.Token);

        // Create KeyVaultManagementClient 
        _keyVaultManagementClient = new KeyVaultManagementClient(credentials);
        // and set the subscription id.
        _keyVaultManagementClient.SubscriptionId = configuration["SUBSCRIPTION_ID"];
    }
    public async Task<bool> DoesKeyVaultHavePurgeProtectionAsync()
    {
        // Retrieve the Key Vault
        var vault = await _keyVaultManagementClient.Vaults.GetAsync(
            _configuration["RESOURCE_GROUP_NAME"], 
            _configuration["KV_RESOURCE_NAME"]
        );

        if (vault == null)
        {
            // Return false if the vault is not found.
            return false;
        }
        
        // Return true if the property: EnablePurgeProtection is found and is true.
        return vault.Properties.EnablePurgeProtection.HasValue &&
               vault.Properties.EnablePurgeProtection.Value;
    }
}