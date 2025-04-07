using Infrastructure.Models;

namespace API.Models;

/// <summary>
/// The request body for the import key operation
/// </summary>
public abstract class ImportKeyRequest
{
    /// <summary>
    /// The name of the new key
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// The action groups that should be alerted on changes
    /// </summary>
    public required IEnumerable<string> ActionGroups { get; init; } 
    
    /// <summary>
    /// The valid key operations
    /// </summary>
    public required string[] KeyOperations { get; init; }
    
    public required DateTime CreatedDate { get; init; }
    
    public required string SignatureBase64 { get; init; }
 }

/// <summary>
/// The request body for the import key operation when the customer is uploading an
/// encrypted key
/// </summary>
public class ImportEncryptedKeyRequest : ImportKeyRequest
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
/// The request body for the import key operation when the customer is uploading an
/// encrypted key
/// </summary>
public class ImportKeyBlobRequest : ImportKeyRequest
{
    /// <summary>
    /// The encypted key material - stored in a ket transfer blob
    /// </summary>
    public required KeyTransferBlob KeyTransferBlob { get; init; }
}