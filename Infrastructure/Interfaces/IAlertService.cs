using Azure.ResourceManager.Monitor;
using Infrastructure.Models;

namespace Infrastructure.Interfaces;

public interface IAlertService
{
    public Task<ScheduledQueryRuleResource> CreateAlertForKeyAsync(string alertName, string keyIdentifier,
        IEnumerable<string> actionGroups);

    public Task<ActionGroupResource> CreateActionGroup(string name, IEnumerable<EmailReceiver> emails);

    public Task<ActionGroupResource> GetActionGroup(string actionGroupName);
}