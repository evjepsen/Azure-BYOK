namespace API.Models;

/// <summary>
/// Interface for requests that need to specify action groups
/// </summary>
public interface IActionGroupsRequest
{
    /// <summary>
    /// The action groups that should be added to alert
    /// </summary>
    IEnumerable<string> ActionGroups { get; }
}