using Azure.ResourceManager.Monitor;
using Infrastructure.Models;

namespace Infrastructure.Interfaces;

/// <summary>
/// Service used to configure alerts for the keys
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Create a new alert for a key stored in the key vault
    /// </summary>
    /// <param name="alertName">The name of the new alert</param>
    /// <param name="keyIdentifier">The identifier of the key that the alert is created for</param>
    /// <param name="actionGroups">The action groups that should be notified when this alert triggers</param>
    /// <returns>The created alert</returns>
    public Task<ScheduledQueryRuleResource> CreateAlertForKeyAsync(string alertName, string keyIdentifier,
        IEnumerable<string> actionGroups);

    /// <summary>
    /// Creates an alert that monitors administrative actions
    /// </summary>
    /// <param name="alertName">The name of the alert</param>
    /// <param name="actionGroups">The action groups that should be alerted on changes</param>
    /// <returns>The created alert</returns>
    public Task<ActivityLogAlertResource> CreateAlertForKeyVaultAsync(string alertName, IEnumerable<string> actionGroups);

    /// <summary>
    /// Check whether there exists an alert for activity happening on the key vault
    /// </summary>
    /// <returns>A boolean denoting whether such an alert exists</returns>
    public Task<bool> CheckForKeyVaultAlertAsync();

    /// <summary>
    /// Creates a new action group
    /// </summary>
    /// <param name="name">Name of the new action group</param>
    /// <param name="emails">List of the emails that should receive the alert</param>
    /// <returns>The created action group</returns>
    public Task<ActionGroupResource> CreateActionGroupAsync(string name, IEnumerable<EmailReceiver> emails);

    /// <summary>
    /// Gets a single action group
    /// </summary>
    /// <param name="actionGroupName">The name of the action group</param>
    /// <returns>The action group with the given name</returns>
    public Task<ActionGroupResource> GetActionGroupAsync(string actionGroupName);
}