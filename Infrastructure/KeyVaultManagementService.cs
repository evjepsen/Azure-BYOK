using System.Collections.Concurrent;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.KeyVault;
using Infrastructure.Exceptions;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Infrastructure;

public class KeyVaultManagementService : IKeyVaultManagementService
{
    private readonly KeyVaultResource _keyVaultResource;
    private readonly ILogger<KeyVaultManagementService> _logger;
    private readonly Dictionary<string, string> _roleDefinitionMap;
    private readonly ConcurrentDictionary<string, string> _displayNameCache;
    private readonly GraphServiceClient _graphClient;

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
        var subscriptionResource = armClient
            .GetSubscriptionResource(subscriptionIdentifier);
        
        var resourceGroupResponse = subscriptionResource
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
        
        // Create the role assignments dictionary
        _roleDefinitionMap = new Dictionary<string, string>();
        var roleDefinitions = subscriptionResource.GetAuthorizationRoleDefinitions();

        foreach (var role in roleDefinitions)
        {
            _roleDefinitionMap[role.Id.Name] = role.Data.RoleName;
        }
        
        // Create the graph client
        var scopes = new[] { "https://graph.microsoft.com/.default" };
        _graphClient = new GraphServiceClient(credential, scopes);
        _displayNameCache = new ConcurrentDictionary<string, string>();
    }

    public bool DoesKeyVaultHavePurgeProtection()
    {
        _logger.LogInformation("Checking if the key vault has purge protection enabled");
        return _keyVaultResource.Data.Properties.EnablePurgeProtection.GetValueOrDefault(false);
    }

    public bool DoesKeyVaultHaveSoftDeleteEnabled()
    {
        _logger.LogInformation("Checking if the key vault has soft delete enabled");
        return _keyVaultResource.Data.Properties.EnableSoftDelete.GetValueOrDefault(true);
    }

    public async Task<IEnumerable<RoleAssignmentDetails>> GetRoleAssignmentsAsync()
    {
        // Get the role assignments collection
        _logger.LogInformation("Getting role assignments");
        var roleAssignments = _keyVaultResource.GetRoleAssignments();
        
        // Create a list to store the results
        var assignmentsList = new List<RoleAssignmentDetails>();
        
        // Get all pages of role assignments
        _logger.LogInformation("Getting all individual role assignments");
        await foreach (var assignment in roleAssignments.GetAllAsync())
        {
            if (!assignment.HasData)
            {
                throw new ResourceNotFoundException($"Failed in getting role assignment data for {assignment.Id}");
            }
            
            if (!_roleDefinitionMap.TryGetValue(assignment.Data.RoleDefinitionId.Name, out var roleName))
            {
                throw new ResourceNotFoundException($"Role definition '{assignment.Data.RoleDefinitionId.Name}' not found");
            }
            
            // Get the name of the principal
            var principalId = assignment.Data.PrincipalId.ToString();
            string displayName;
            
            if (string.IsNullOrEmpty(principalId))
            {
                _logger.LogWarning("Assignment {id} has no PrincipalId; showing as Unknown", assignment.Id);
                displayName = "<Unknown principal>";
            }
            else
            {
                displayName = _displayNameCache.GetOrAdd(principalId, key =>
                {
                    var directoryObject = _graphClient
                        .DirectoryObjects[key]
                        .GetAsync()
                        .GetAwaiter()
                        .GetResult();

                    if (directoryObject == null)
                    {
                        _logger.LogWarning("Principal {key} not found in Graph API", key);
                        throw new ResourceNotFoundException($"Failed to retrieve principal {key}");
                    }

                    return directoryObject switch
                    {
                        User u => u.DisplayName ?? key,
                        ServicePrincipal sp => sp.DisplayName ?? key,
                        _ => key
                    };
                });
            }
            
            // Create the role assignment details object
            var roleAssignmentDetails = new RoleAssignmentDetails
            {
                RoleName = roleName,
                RoleId = assignment.Data.RoleDefinitionId.Name,
                PrincipalId = principalId,
                PrincipalName = displayName,
                CreatedOn = assignment.Data.CreatedOn,
                CreatedBy = assignment.Data.CreatedBy,
                Description = assignment.Data.Description,
                Scope = assignment.Data.Scope,
            };
            
            assignmentsList.Add(roleAssignmentDetails);
        }
    
        return assignmentsList;
    }
}