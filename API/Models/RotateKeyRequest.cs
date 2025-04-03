namespace API.Models;

/// <summary>
/// The request body for the rotate key operation
/// </summary>
public class RotateKeyRequest
{
    /// <summary>
    /// The name of the new key
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// The encypted key material - base64 encoded
    /// </summary>
    public required string EncryptedKeyBase64 { get; init; }
    
    /// <summary>
    /// The id of the key encryption key used to encrypt the user specified key
    /// </summary>
    public required string KeyEncryptionKeyId { get; init; }
}