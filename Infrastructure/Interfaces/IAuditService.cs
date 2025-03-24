namespace Infrastructure.Interfaces;

/// <summary>
/// Service used to interact with the key vault to access logs
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// View the key operations performed on the keys stored in the vault 
    /// </summary>
    /// <param name="numOfDays">The time period to get logs in days</param>
    /// <returns>The key operation log entries for the specified period</returns>
    public Task<string> GetKeyOperationsPerformedAsync(int numOfDays);

    /// <summary>
    /// View the vault operations performed on the vault. Source is the AzureDiagnostics table
    /// </summary>
    /// <param name="numOfDays">The time period to get logs in days</param>
    /// <returns>The vault operation log entries for the specified period</returns>
    public Task<string> GetVaultOperationsPerformedAsync(int numOfDays);
    
    /// <summary>
    /// View the activity on the key vault. Including updates to the key vault
    /// and role assignments. Source is the Activity Log
    /// </summary>
    /// <param name="numOfDays">The time period to get logs in days</param>
    /// <returns>The activity log entries for the specified period</returns>
    public Task<string> GetKeyVaultActivityLogsAsync(int numOfDays);
}