using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Infrastructure.Exceptions;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure;

public class KeyVaultManagementService : IKeyVaultManagementService
{
    private readonly KeyVaultResource _keyVaultResource;
    
    public KeyVaultManagementService(IConfiguration configuration)
    {
        var subscriptionId =    configuration["SUBSCRIPTION_ID"] ?? throw new EnvironmentVariableNotSetException("The Subscription Id was not set");
        var resourceGroupName = configuration["RESOURCE_GROUP_NAME"] ?? throw new EnvironmentVariableNotSetException("The Resource Group Name was not set");
        var keyVaultResourceName = configuration["KV_RESOURCE_NAME"] ?? throw new EnvironmentVariableNotSetException("The Resource Name was not set");
        
        // Get management token.
        var credential = new DefaultAzureCredential();
        var armClient = new ArmClient(credential);
        var subscriptionIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
        
        // Get the Key Vault Management client.
        var resourceGroupResponse = armClient
            .GetSubscriptionResource(subscriptionIdentifier)
            .GetResourceGroup(resourceGroupName);
        
        if (!resourceGroupResponse.HasValue)
        {
            throw new ResourceNotFoundException($"Resource group '{resourceGroupName}' not found in subscription '{subscriptionId}'");
        }
        
        var keyVaultResponse = resourceGroupResponse.Value.GetKeyVault(keyVaultResourceName);
        
        // Check that the key vault exists
        if (!keyVaultResponse.HasValue)
        {
            throw new ResourceNotFoundException($"Key Vault '{keyVaultResourceName}' not found in resource group '{resourceGroupName}'");
        }
        
        _keyVaultResource = keyVaultResponse.Value;
    }

    public bool DoesKeyVaultHavePurgeProtection()
    {
        return _keyVaultResource.Data.Properties.EnablePurgeProtection.GetValueOrDefault(false);
    }
}