using Infrastructure.Models;

namespace API.Models;

/// <summary>
/// The request body for the rotate key operation
/// </summary>
public abstract class RotateKeyRequest
{
    /// <summary>
    /// The name of the key
    /// </summary>
    public required string Name { get; init; }
}

/// <summary>
/// The request body for the rotate key operation when the customer is uploading an
/// encrypted key
/// </summary>
public class RotateEncryptedKeyRequest : RotateKeyRequest
{
    /// <summary>
    /// The id of the key encryption key used to encrypt the user specified key
    /// </summary>
    public required string KeyEncryptionKeyId { get; init; }
    
    /// <summary>
    /// The encypted key material - base64 encoded
    /// </summary>
    public required string EncryptedKeyBase64 { get; init; }
}

/// <summary>
/// The request body for the rotate key operation when the customer is uploading an
/// encrypted key
/// </summary>
public class RotateKeyBlobRequest : RotateKeyRequest
{
    /// <summary>
    /// The encypted key material - stored in a ket transfer blob
    /// </summary>
    public required KeyTransferBlob KeyTransferBlob { get; init; }
}