using Azure.Security.KeyVault.Keys;

namespace Infrastructure.Interfaces;

public interface IDeletedKeyWrapper
{
    /// <summary>
    /// Gets when the key was deleted.
    /// </summary>
    DateTimeOffset? DeletedOn { get; }
        
    /// <summary>
    /// Gets the key identifier.
    /// </summary>
    Uri Id { get; }
    
    JsonWebKey Key { get; }
        
    /// <summary>
    /// Gets the operations you can perform using the key.
    /// </summary>
    IReadOnlyCollection<KeyOperation> KeyOperations { get; }
        
    /// <summary>
    /// Gets the KeyType for this key.
    /// </summary>
    KeyType KeyType { get; }
        
    /// <summary>
    /// Gets the name of the key.
    /// </summary>
    string Name { get; }
        
    /// <summary>
    /// Gets additional properties of the KeyVaultKey.
    /// </summary>
    KeyProperties Properties { get; }
        
    /// <summary>
    /// Gets a Uri of the deleted key that can be used to recover it.
    /// </summary>
    Uri RecoveryId { get; }
        
    /// <summary>
    /// Gets when the deleted key will be purged.
    /// </summary>
    DateTimeOffset? ScheduledPurgeDate { get; }
}