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

}