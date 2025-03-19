using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Monitor;
using Azure.ResourceManager.Monitor.Models;
using Infrastructure.Exceptions;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Microsoft.Extensions.Configuration;

namespace Infrastructure;

public class AlertService : IAlertService
{
    private readonly ArmClient _armClient;
    private readonly ResourceIdentifier _subscriptionIdentifier;
    private readonly string _subscriptionId;
    private readonly string _resourceGroupName;
    private readonly string _keyVaultResourceId;

    public AlertService(IConfiguration configuration)
    {
        TokenCredential credential = new DefaultAzureCredential();
        _armClient = new ArmClient(credential);

        // Save the id's needed
        _subscriptionId =    configuration["SUBSCRIPTION_ID"] ?? throw new EnvironmentVariableNotSetException("The Subscription Id was not set");
        _resourceGroupName = configuration["RESOURCE_GROUP_NAME"] ?? throw new EnvironmentVariableNotSetException("The Resource Group Name was not set");
        var keyVaultResource = configuration["KV_RESOURCE_NAME"] ?? throw new EnvironmentVariableNotSetException("The Resource Name was not set");
        
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
        var subscription = _armClient.GetSubscriptionResource(_subscriptionIdentifier);
        var resourceGroup = await subscription.GetResourceGroupAsync(_resourceGroupName);
        var alertRules = resourceGroup.Value.GetScheduledQueryRules();
        var newAlertOperation = await alertRules.CreateOrUpdateAsync(
            WaitUntil.Completed,
            alertName,
            alert);
        
        // Wait for it to be added
        var newAlert = await newAlertOperation.WaitForCompletionAsync();

        if (!newAlert.HasValue)
        {
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
        var subscription = _armClient.GetSubscriptionResource(_subscriptionIdentifier);
        var resourceGroup = await subscription.GetResourceGroupAsync(_resourceGroupName);
        var alertRules = resourceGroup.Value.GetActivityLogAlerts();
        var newAlertOperation = await alertRules.CreateOrUpdateAsync(
            WaitUntil.Completed,
            alertName,
            alert
        );
        
        // Wait for it to be added
        var newAlert = await newAlertOperation.WaitForCompletionAsync();

        if (!newAlert.HasValue)
        {
            throw new HttpRequestException("Could not add the new activity alert");
        }

        return newAlert.Value;
        
    }

    public Task<bool> CheckForKeyVaultAlert()
    {
        throw new NotImplementedException();
    }

    public async Task<ActionGroupResource> CreateActionGroupAsync(string name, IEnumerable<EmailReceiver> emails)
    {
        // Get the subscription
        var subscription = _armClient.GetSubscriptionResource(_subscriptionIdentifier);
        
        // Get the resource group with the key vault
        var resourceGroup = await subscription.GetResourceGroupAsync(_resourceGroupName);

        if (!resourceGroup.HasValue)
        {
            throw new HttpRequestException("Could not access the resource group");
        }
        
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
        var actionGroups = resourceGroup.Value.GetActionGroups();
        var newActionGroupOperation = await actionGroups.CreateOrUpdateAsync(
            WaitUntil.Completed,
            name,
            actionGroupData
        );
        
        // Wait for the action group to be added
        var newActionGroup = await newActionGroupOperation.WaitForCompletionAsync();
        
        if (!newActionGroup.HasValue)
        {
            throw new HttpRequestException("Could not add the new action group");
        }
        
        return newActionGroup.Value;
    }

    public async Task<ActionGroupResource> GetActionGroupAsync(string actionGroupName)
    {
        var subscription = _armClient.GetSubscriptionResource(_subscriptionIdentifier);
        var resourceGroup = await subscription.GetResourceGroupAsync(_resourceGroupName);
        
        var actionGroup = await resourceGroup.Value.GetActionGroupAsync(actionGroupName);
        return actionGroup;
    }
}