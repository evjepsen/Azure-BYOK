using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Monitor;
using Azure.ResourceManager.Monitor.Models;
using Azure.ResourceManager.Resources;
using Infrastructure.Exceptions;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public class AlertService : IAlertService
{
    private readonly ArmClient _armClient;
    private readonly ResourceIdentifier _subscriptionIdentifier;
    private readonly string _subscriptionId;
    private readonly string _resourceGroupName;
    private readonly string _keyVaultResourceId;
    private readonly ILogger<AlertService> _logger;

    public AlertService(IHttpClientFactory httpClientFactory, IOptions<ApplicationOptions> applicationOptions, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<AlertService>();
        
        TokenCredential credential = new DefaultAzureCredential();

        // Save the id's needed
        _subscriptionId = applicationOptions.Value.SubscriptionId;
        _resourceGroupName = applicationOptions.Value.ResourceGroupName;
        var keyVaultResource = applicationOptions.Value.KeyVaultResourceName;
        
        _armClient = new ArmClient(credential, _subscriptionId, new ArmClientOptions
        {
            Transport = new HttpClientTransport(httpClientFactory.CreateClient("WaitAndRetry"))
        });
        
        // Set up the resource id
        _keyVaultResourceId = $"/subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultResource}";
        // Set up the subscription id
        _subscriptionIdentifier = new ResourceIdentifier($"/subscriptions/{_subscriptionId}");

    }

    public async Task<ScheduledQueryRuleResource> CreateAlertForKeyAsync(string alertName, string keyIdentifier, IEnumerable<string> actionGroups)
    {
        // Create the query
        var query = $"AzureDiagnostics | where OperationName startswith \"key\" | where id_s has \"{keyIdentifier}\"";
        // Set up the alert
        // Add the action group
        var alertAction = new ScheduledQueryRuleActions();
        foreach (var actionGroup in actionGroups)
        {
            alertAction.ActionGroups.Add($"/subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroupName}/providers/microsoft.insights/actionGroups/{actionGroup}");
        }
        
        // Set up the alert conditions
        var alertConditions = new ScheduledQueryRuleCondition
        {
            Query = query,
            TimeAggregation = ScheduledQueryRuleTimeAggregationType.Count,
            Operator = MonitorConditionOperator.GreaterThanOrEqual,
            Threshold = 1,
            FailingPeriods = new ConditionFailingPeriods
            {
                NumberOfEvaluationPeriods = 1,
                MinFailingPeriodsToAlert = 1
            },
            ResourceIdColumn = "ResourceId"
        };
        
        // Create the alert item
        var alert = new ScheduledQueryRuleData(AzureLocation.NorthEurope)
        {
            Actions = alertAction,
            DisplayName = alertName,
            Description = $"Alert for BYOK key: {keyIdentifier}",
            IsEnabled = true,
            Severity = 1,
            WindowSize = TimeSpan.FromMinutes(10),
            Scopes = { _keyVaultResourceId },
            CriteriaAllOf = { alertConditions },
            EvaluationFrequency = TimeSpan.FromMinutes(5)
        };
        
        // Add the new alert
        var resourceGroup = await GetResourceGroupAsync();
        
        _logger.LogInformation("Adding new log alert for usage of key with id: {keyIdentifier}", keyIdentifier);
        var alertRules = resourceGroup.GetScheduledQueryRules();
        var newAlertOperation = await alertRules.CreateOrUpdateAsync(
            WaitUntil.Completed,
            alertName,
            alert);
        
        // Wait for it to be added
        var newAlert = await newAlertOperation.WaitForCompletionAsync();

        if (!newAlert.HasValue)
        {
            _logger.LogError("Could not add the new log alert");
            throw new HttpRequestException("Could not add the new log alert");
        }

        return newAlert.Value;
    }

    public async Task<ActivityLogAlertResource> CreateAlertForKeyVaultAsync(string alertName, IEnumerable<string> actionGroups)
    {
        // Set up the conditions
        var conditions = new List<ActivityLogAlertAnyOfOrLeafCondition>
        {
            // Get administrative actions
            new()
            {
                Field = "category",
                EqualsValue = "Administrative"
            },
            new()
            {
                AnyOf =
                {
                    // Get the actions performed on the key vault
                    new AlertRuleLeafCondition
                    {
                        Field = "resourceType",
                        EqualsValue = "Microsoft.KeyVault/vaults"
                    },
                    // Get the actions that updated roles 
                    new AlertRuleLeafCondition
                    {
                        Field = "resourceType",
                        EqualsValue = "Microsoft.Authorization/roleAssignments"  
                    }
                }
            }
        };
        
        // Create the alert
        var alert = new ActivityLogAlertData("Global")
        {
            IsEnabled = true,
            Scopes = { _keyVaultResourceId },
            ConditionAllOf = conditions,
            Description = "Ensure that changes to the Key Vault and corresponding role assignments are alerted"
        };
        
        // Add the action groups to the new alert
        foreach (var actionGroupName in actionGroups)
        {
            var actionGroupIdentifier = new ResourceIdentifier($"/subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroupName}/providers/microsoft.insights/actionGroups/{actionGroupName}");
            var actionGroup = new ActivityLogAlertActionGroup(actionGroupIdentifier);
            alert.ActionsActionGroups.Add(actionGroup);
        }
        
        // Add the new alert
        _logger.LogInformation("Adding new activity alert for the key vault");
        var resourceGroup = await GetResourceGroupAsync();
        var alertRules = resourceGroup.GetActivityLogAlerts();
        var newAlertOperation = await alertRules.CreateOrUpdateAsync(
            WaitUntil.Completed,
            alertName,
            alert
        );
        
        // Wait for it to be added
        var newAlert = await newAlertOperation.WaitForCompletionAsync();

        if (!newAlert.HasValue)
        {
            _logger.LogError("Could not add the new activity alert");
            throw new HttpRequestException("Could not add the new activity alert");
        }

        return newAlert.Value;
        
    }

    public async Task<bool> CheckForKeyVaultAlertAsync()
    {
        // Get all the activity alerts
        _logger.LogInformation("Retrieving all activity alerts");
        var resourceGroup = await GetResourceGroupAsync();
        var alertRules = resourceGroup
            .GetActivityLogAlerts()
            .GetAllAsync();

        // Check that at least one is for the key vault
        _logger.LogInformation("Checking that there is a key vault activity alert");
        var isThereKeyVaultAlert = false;
        await foreach (var alert in alertRules)
        {
            if (!alert.HasData)
            {
                continue;
            }
            
            var isEnabled = alert.Data.IsEnabled.HasValue && alert.Data.IsEnabled.Value;
            var isForCorrectResource = alert.Data.Scopes.Contains(_keyVaultResourceId);
            
            // Check that it has the correct conditions
            var hasAdministrativeCategory = alert
                .Data
                .ConditionAllOf
                .Any(condition => condition.Field == "category" && condition.EqualsValue == "Administrative");
            
            var hasRequiredResourceType = alert
                .Data
                .ConditionAllOf
                .Any(condition =>
                {
                    var isThereVaultCondition = condition.AnyOf.Any(leafCondition => leafCondition.Field == "resourceType" && leafCondition.EqualsValue == "Microsoft.KeyVault/vaults");
                    var isThereAuthorizationCondition = condition.AnyOf.Any(leafCondition => leafCondition.Field == "resourceType" && leafCondition.EqualsValue == "Microsoft.Authorization/roleAssignments" );
                    return isThereVaultCondition && isThereAuthorizationCondition;
                });
            
            // We are satisfied when a proper alert has been found
            isThereKeyVaultAlert = isEnabled && isForCorrectResource && hasAdministrativeCategory && hasRequiredResourceType;
            if (isThereKeyVaultAlert)
            {
                return true;
            } 
        }

        return isThereKeyVaultAlert;
    }

    public async Task<ActionGroupResource> CreateActionGroupAsync(string name, IEnumerable<EmailReceiver> emails)
    {
        var resourceGroup = await GetResourceGroupAsync();
        
        var actionGroupData = new ActionGroupData("Global")
        {
            GroupShortName = name,
            IsEnabled = true
        };
        
        // Add the email receivers
        foreach (var email in emails)
        {
            var emailReceiver = new MonitorEmailReceiver(email.Name, email.Email);
            actionGroupData.EmailReceivers.Add(emailReceiver);
        }
        
        // Add the action group    
        _logger.LogInformation("Creating a new action group with name: {name}", name);
        var actionGroups = resourceGroup.GetActionGroups();
        var newActionGroupOperation = await actionGroups.CreateOrUpdateAsync(
            WaitUntil.Completed,
            name,
            actionGroupData
        );
        
        // Wait for the action group to be added
        var newActionGroup = await newActionGroupOperation.WaitForCompletionAsync();
        
        if (!newActionGroup.HasValue)
        {
            _logger.LogError("Failed to create the new action group");   
            throw new HttpRequestException("Failed to create the new action group");
        }
        
        return newActionGroup.Value;
    }

    public async Task<ActionGroupResource> GetActionGroupAsync(string actionGroupName)
    {
        var resourceGroup = await GetResourceGroupAsync();
        _logger.LogInformation("Retrieving the action group {name}", actionGroupName);
        var actionGroup = await resourceGroup.GetActionGroupAsync(actionGroupName);
        
        if (!actionGroup.HasValue)
        {
            _logger.LogError("Could not access the action group");
            throw new HttpRequestException("Could not access the action group");
        }
        
        return actionGroup.Value;
    }
    
    // Helper methods
    private async Task<ResourceGroupResource> GetResourceGroupAsync()
    {
        _logger.LogInformation("Retrieving the resource group");
        var resourceGroup = await _armClient
            .GetSubscriptionResource(_subscriptionIdentifier)
            .GetResourceGroupAsync(_resourceGroupName);

        if (!resourceGroup.HasValue)
        {
            _logger.LogError("Could not access the resource group");
            throw new HttpRequestException("Could not access the resource group");
        }

        return resourceGroup.Value;
    }
}