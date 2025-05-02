using Infrastructure.Models;

namespace API.Models;

/// <summary>
/// A base class for all key requests
/// </summary>
public abstract class KeyRequestBase
{
    /// <summary>
    /// The name of the key
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// The valid key operations
    /// </summary>
    public required string[] KeyOperations { get; init; }
    
    /// <summary>
    /// The timestamp of sending the request
    /// </summary>
    public required DateTime TimeStamp { get; set; }
    
    /// <summary>
    /// The signature in base64 format
    /// </summary>
    public required string SignatureBase64 { get; init; }
}

/// <summary>
/// Base class for requests using encrypted key material
/// </summary>
public abstract class EncryptedKeyRequestBase : KeyRequestBase
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
/// Base class for requests using encrypted key material
/// </summary>
public abstract class KeyBlobRequestBase : KeyRequestBase
{
    /// <summary>
    /// The encypted key material - stored in a ket transfer blob
    /// </summary>
    public required KeyTransferBlob KeyTransferBlob { get; init; }
}