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
    private ArmClient _armClient;

    public AlertService()
    {
        TokenCredential credential = new DefaultAzureCredential();
        _armClient = new ArmClient(credential);
    }

    public Task<string> CreateAlertForKeyAsync(string keyIdentifier)
    {
        throw new NotImplementedException();
    }

    public async Task<ActionGroupResource> CreateActionGroup(string name, IEnumerable<EmailReceiver> emails)
    {
        // Get the subscription
        var subscriptionId = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID") ?? throw new EnvironmentVariableNotSetException("The Subscription Id was not set");
        var subscription = _armClient.GetSubscriptionResource(
            new ResourceIdentifier($"/subscriptions/{subscriptionId}")
        );
        
        // Get the resource group with the key vault
        var resourceGroup = await subscription.GetResourceGroupAsync("BYOK");

        if (!resourceGroup.HasValue)
        {
            throw new HttpRequestException("Could not access the resource group");
        }

        ActionGroupData actionGroupData = new ActionGroupData("Global")
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
            
        var actionGroups = resourceGroup.Value.GetActionGroups();
        var newActionGroupOperation = await actionGroups.CreateOrUpdateAsync(
            WaitUntil.Completed,
            name,
            actionGroupData
        );

        var newActionGroup = await newActionGroupOperation.WaitForCompletionAsync();
        
        if (!newActionGroup.HasValue)
        {
            throw new HttpRequestException("Could not add the new action group");
        }
        
        return newActionGroup.Value;
        
    }
}