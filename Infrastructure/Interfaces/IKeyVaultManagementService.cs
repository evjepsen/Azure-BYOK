using Azure.ResourceManager.Authorization;
using Infrastructure.Models;

namespace Infrastructure.Interfaces;
/// <summary>
/// Interface for the Key Vault Management Service.
/// Used to interact with the Azure Key Vault Management API
/// Can be used to get properties associated with the Key Vault
/// </summary>
public interface IKeyVaultManagementService
{
    /// <summary>
    /// Method to check if the Key Vault has purge protection enabled
    /// </summary>
    /// <returns> Returns true if and only if the Key Vault has purge protection enabled, false otherwise </returns>
    public bool DoesKeyVaultHavePurgeProtection();
    
    /// <summary>
    /// Method to check if the Key Vault has soft delete enabled
    /// </summary>
    /// <returns>Returns true if and only if the Key vault has soft delete enabled, false otherwise</returns>
    public bool DoesKeyVaultHaveSoftDeleteEnabled();

    /// <summary>
    /// Method to check which role assignments are made on the key vault
    /// </summary>
    /// <returns>A collection of the role assignments</returns>
    public Task<IEnumerable<RoleAssignmentDetails>> GetRoleAssignmentsAsync();
}