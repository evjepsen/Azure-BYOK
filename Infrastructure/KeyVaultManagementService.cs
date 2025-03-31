using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Infrastructure.Exceptions;
using Infrastructure.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public class KeyVaultManagementService : IKeyVaultManagementService
{
    private readonly KeyVaultResource _keyVaultResource;
    private readonly ILogger<KeyVaultManagementService> _logger;

    public KeyVaultManagementService(IOptions<ApplicationOptions> applicationOptions, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<KeyVaultManagementService>();
        
        var subscriptionId = applicationOptions.Value.SubscriptionId;
        var resourceGroupName = applicationOptions.Value.ResourceGroupName;
        var keyVaultResourceName = applicationOptions.Value.KeyVaultResourceName;
        
        // Get management token.
        var credential = new DefaultAzureCredential();
        var armClient = new ArmClient(credential);
        var subscriptionIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
        
        // Get the Key Vault Management client.
        _logger.LogInformation("Accessing the resource group");
        var resourceGroupResponse = armClient
            .GetSubscriptionResource(subscriptionIdentifier)
            .GetResourceGroup(resourceGroupName);
        
        if (!resourceGroupResponse.HasValue)
        {
            _logger.LogCritical("Resource group '{resourceGroupName}' not found in subscription '{subscriptionId}'", resourceGroupName, subscriptionId);
            throw new ResourceNotFoundException($"Resource group '{resourceGroupName}' not found in subscription '{subscriptionId}'");
        }
        
        _logger.LogInformation("Accessing the key vault");
        var keyVaultResponse = resourceGroupResponse.Value.GetKeyVault(keyVaultResourceName);
        
        // Check that the key vault exists
        if (!keyVaultResponse.HasValue)
        {
            _logger.LogCritical("Key Vault '{keyVaultResourceName}' not found in resource group '{resourceGroupName}'", keyVaultResourceName, resourceGroupName);
            throw new ResourceNotFoundException($"Key Vault '{keyVaultResourceName}' not found in resource group '{resourceGroupName}'");
        }
        
        _keyVaultResource = keyVaultResponse.Value;
    }

    public bool DoesKeyVaultHavePurgeProtection()
    {
        _logger.LogInformation("Checking if the key vault has purge protection enabled");
        return _keyVaultResource.Data.Properties.EnablePurgeProtection.GetValueOrDefault(false);
    }
}