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
    /// <param name="encryptedData">The BYOK TDE protector encrypted under KEK</param>
    /// <param name="kekId">Key identifier of the KEK used</param>
    /// <returns>The public information of the Azure Key Vault key that has been uploaded</returns>
    public Task<KeyVaultUploadKeyResponse> UploadKey(string name, byte[] encryptedData, string kekId);

    /// <summary>
    /// Generate a Key Encryption Key (KEK) to protect the customer's TDE protector
    /// </summary>
    /// <param name="name">Name of the new KEK</param>
    /// <returns>The KEK as an Azure Key Vault Key</returns>
    public Task<KeyVaultKey> GenerateKekAsync(string name);
    
    /// <summary>
    /// Download public key of the KEK in PEM format
    /// </summary>
    /// <param name="kekId">id of the KEK</param>
    /// <returns>A public key in pem format</returns>
    public Task<PublicKeyKekPem> DownloadPublicKekAsPemAsync(string kekId);


    /// <summary>
    /// Asynchronously delete a key encryption key
    /// </summary>
    /// <param name="kekId"></param>
    /// <returns> The response message</returns>
    public Task<DeleteKeyOperation> DeleteKekAsync(string kekId);
    /// <summary>
    /// Purge a deleted key
    /// </summary>
    /// <param name="kekId"></param>
    /// <returns> The response message </returns>
    public Task<Response> PurgeDeletedKekAsync(string kekId);
}