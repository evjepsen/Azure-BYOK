using Azure;
using Azure.Security.KeyVault.Keys;

namespace Infrastructure.Interfaces;

/// <summary>
/// Service used to interact with the key vault (and HSM) hosted on Azure
/// </summary>
public interface IKeyVaultService
{
    /// <summary>
    /// Upload the customer BYOK TDE protector
    /// </summary>
    /// <param name="name">Name of the TDE protector</param>
    /// <param name="encryptedData">The BYOK TDE protector encrypted under KEK</param>
    /// <param name="kekId">Key identifier of the KEK used</param>
    /// <returns>The public information of the Azure Key Vault key that has been uploaded</returns>
    public Task<string> UploadKey(string name, byte[] encryptedData, string kekId);

    /// <summary>
    /// Generate a Key Encryption Key (KEK) to protect the customer's TDE protector
    /// </summary>
    /// <param name="name">Name of the new KEK</param>
    /// <returns>The KEK as an Azure Key Vault Key</returns>
    public Response<KeyVaultKey> GenerateKek(string name);
    
    
    
}