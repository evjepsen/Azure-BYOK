using Infrastructure.Models;

namespace Infrastructure.Interfaces;

/// <summary>
/// Used to generate the different tokens used to handle key upload
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a key transfer blob to upload the user specified key 
    /// </summary>
    /// <param name="encryptedKey">The user specified key in encrypted format</param>
    /// <param name="kekId">The id of the key encryption key used to encrypt the user specified key</param>
    /// <returns>The key transfer blob to be uploaded to the Azure Key Vault</returns>
    public KeyTransferBlob CreateKeyTransferBlob(byte[] encryptedKey, string kekId);

    /// <summary>
    /// Creates the request body for the key import operation
    /// </summary>
    /// <param name="transferBlob">The key transfer blob containing the key to be uploaded</param>
    /// <param name="keyOperations">The operations the new key can perform</param>
    /// <returns>The request body for the upload request</returns>
    public UploadKeyRequestBody CreateBodyForRequest(KeyTransferBlob transferBlob, string[] keyOperations);
}