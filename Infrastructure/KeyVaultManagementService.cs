using Azure.Core;
using Azure.Identity;
using Infrastructure.Interfaces;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest;

namespace Infrastructure;

public class KeyVaultManagementService(IConfiguration configuration) : IKeyVaultManagementService
{
    public async Task<bool> DoesKeyVaultHavePurgeProtectionAsync()
    {
        // Get management token.
        var tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions());
        var tokenRequestContext = new TokenRequestContext(["https://management.azure.com/.default"]);
        var accessToken = await tokenCredential.GetTokenAsync(tokenRequestContext);
        // Create ServiceClientCredentials credentials.
        ServiceClientCredentials credentials = new TokenCredentials(accessToken.Token);

        // Create KeyVaultManagementClient to retrieve the Key Vault details.
        // has to be disposable
        using var keyVaultManagementClient = new KeyVaultManagementClient(credentials);
        keyVaultManagementClient.SubscriptionId = configuration["SUBSCRIPTION_ID"];
        // Retrieve the Key Vault
        Vault vault = await keyVaultManagementClient.Vaults.GetAsync(
            configuration["RESOURCE_GROUP_NAME"], 
            configuration["KV_RESOURCE_NAME"]
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