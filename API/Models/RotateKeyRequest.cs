namespace API.Models;

/// <summary>
/// The request body for the rotate key operation when the customer is uploading an
/// encrypted key
/// </summary>
public class RotateEncryptedKeyRequest : EncryptedKeyRequestBase;

/// <summary>
/// The request body for the rotate key operation when the customer is uploading an
/// encrypted key
/// </summary>
public class RotateKeyBlobRequestBase : KeyBlobRequestBase;