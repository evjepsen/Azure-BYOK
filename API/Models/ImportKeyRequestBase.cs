namespace API.Models;

/// <summary>
/// The request body for the import key operation when the customer is uploading an
/// encrypted key
/// </summary>
public class ImportEncryptedKeyRequest : EncryptedKeyRequestBase, IActionGroupsRequest
{   
    /// <summary>
    /// The action groups that should be added to alert
    /// </summary>
    public IEnumerable<string> ActionGroups { get; set; } = [];
}

/// <summary>
/// The request body for the import key operation when the customer is uploading an
/// encrypted key
/// </summary>
public class ImportKeyBlobRequest : KeyBlobRequestBase, IActionGroupsRequest
{
    /// <summary>
    /// The action groups that should be added to alert
    /// </summary>
    public IEnumerable<string> ActionGroups { get; init; } = [];
}