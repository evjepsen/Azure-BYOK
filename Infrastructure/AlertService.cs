using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Monitor;
using Azure.ResourceManager.Monitor.Models;
using Infrastructure.Exceptions;
using Infrastructure.Interfaces;
using Infrastructure.Models;

namespace Infrastructure;

public class AlertService : IAlertService
{
    private readonly ArmClient _armClient;
    private readonly ResourceIdentifier _subscriptionIdentifier;
    private readonly string _subscriptionId;
    private readonly string _resourceGroupName;
    private readonly string _keyVaultResource;

    public AlertService()
    {
        TokenCredential credential = new DefaultAzureCredential();
        _armClient = new ArmClient(credential);
        
        // Save the id's needed
        _subscriptionId = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID") ?? throw new EnvironmentVariableNotSetException("The Subscription Id was not set");
        _resourceGroupName = Environment.GetEnvironmentVariable("RESOURCE_GROUP_NAME") ?? throw new EnvironmentVariableNotSetException("The Resource Group Name was not set");
        _keyVaultResource = Environment.GetEnvironmentVariable("RESOURCE") ?? throw new EnvironmentVariableNotSetException("The Resource Name was not set");
        
        // Setup the resourceId
        _subscriptionIdentifier = new ResourceIdentifier($"/subscriptions/{_subscriptionId}");
    }

    public async Task<ScheduledQueryRuleResource> CreateAlertForKeyAsync(string keyIdentifier,
        IEnumerable<string> actionGroups)
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
            DisplayName = "BYOKAlert",
            Description = $"Alert for BYOK key: {keyIdentifier}",
            IsEnabled = true,
            Severity = 1,
            WindowSize = TimeSpan.FromMinutes(10),
            Scopes = { $"/subscriptions/{_subscriptionId}/resourceGroups/{_resourceGroupName}/providers/Microsoft.KeyVault/vaults/{_keyVaultResource}" },
            CriteriaAllOf = { alertConditions },
            EvaluationFrequency = TimeSpan.FromMinutes(5)
        };
        
        // Add the new alert
        var subscription = _armClient.GetSubscriptionResource(_subscriptionIdentifier);
        var resourceGroup = await subscription.GetResourceGroupAsync("BYOK");
        var alertRules = resourceGroup.Value.GetScheduledQueryRules();
        var newAlertOperation = await alertRules.CreateOrUpdateAsync(
            WaitUntil.Completed,
            "BYOKAlert",
            alert);
        
        // Wait for it to be added
        var newAlert = await newAlertOperation.WaitForCompletionAsync();

        if (!newAlert.HasValue)
        {
            throw new HttpRequestException("Could not add the new action group");
        }

        return newAlert.Value;
    }

    public async Task<ActionGroupResource> CreateActionGroup(string name, IEnumerable<EmailReceiver> emails)
    {
        // Get the subscription
        var subscription = _armClient.GetSubscriptionResource(_subscriptionIdentifier);
        
        // Get the resource group with the key vault
        var resourceGroup = await subscription.GetResourceGroupAsync("BYOK");

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
}