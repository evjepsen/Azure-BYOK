using Azure;
using Azure.Security.KeyVault.Keys;
using Infrastructure.Models;

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
    /// <param name="transferBlobStrategy">The strategy used to create the transfer blob</param>
    /// <param name="keyOperations">The operations allowed on the new key</param>
    /// <returns>The public information of the Azure Key Vault key that has been uploaded</returns>
    public Task<KeyVaultUploadKeyResponse> UploadKey(string name, ITransferBlobStrategy transferBlobStrategy, string[] keyOperations);

    /// <summary>
    /// Generate a Key Encryption Key (KEK) to protect the customer's TDE protector
    /// </summary>
    /// <param name="name">Name of the new KEK</param>
    /// <returns>The KEK as an Azure Key Vault Key</returns>
    public Task<KeyVaultKey> GenerateKekAsync(string name);
    
    /// <summary>
    /// Download public key of the KEK in PEM format
    /// </summary>
    /// <param name="kekName">id of the KEK</param>
    /// <returns>A public key in pem format</returns>
    public Task<PublicKeyKekPem> DownloadPublicKekAsPemAsync(string kekName);

    /// <summary>
    /// Asynchronously delete a key encryption key
    /// </summary>
    /// <param name="keyId">id of the KEK</param>
    /// <returns> The response message</returns>
    public Task<DeletedKey> DeleteKeyAsync(string keyId);
    
    /// <summary>
    /// Purge a deleted key encryption key
    /// </summary>
    /// <param name="keyId">id of the KEK</param>
    /// <returns> The response message </returns>
    public Task<Response> PurgeDeletedKeyAsync(string keyId);

    /// <summary>
    /// Recover a deleted key
    /// </summary>
    /// <param name="keyName">id of the Key</param>
    /// <returns>The response message</returns>
    public Task<RecoverDeletedKeyOperation> RecoverDeletedKeyAsync(string keyName);

    /// <summary>
    /// Checks whether a given key exists in azure
    /// </summary>
    /// <param name="keyName">The name of the key to check</param>
    /// <returns>True when the key exists and false otherwise</returns>
    public Task<bool> CheckIfKeyExistsAsync(string keyName);
}