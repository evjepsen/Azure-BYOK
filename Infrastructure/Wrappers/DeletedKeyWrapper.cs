using Azure.Security.KeyVault.Keys;
using Infrastructure.Interfaces;

namespace Infrastructure.Wrappers;

public class DeletedKeyWrapper : IDeletedKeyWrapper
{
    private readonly DeletedKey _deletedKey;

    public DeletedKeyWrapper(DeletedKey deletedKey)
    {
        _deletedKey = deletedKey;
    }
    
    public DateTimeOffset? DeletedOn => _deletedKey.DeletedOn;

    public Uri Id => _deletedKey.Id;

    public JsonWebKey Key => _deletedKey.Key;

    public IReadOnlyCollection<KeyOperation> KeyOperations => _deletedKey.KeyOperations;

    public KeyType KeyType => _deletedKey.KeyType;

    public string Name => _deletedKey.Name;

    public KeyProperties Properties => _deletedKey.Properties;

    public Uri RecoveryId => _deletedKey.RecoveryId;

    public DateTimeOffset? ScheduledPurgeDate => _deletedKey.ScheduledPurgeDate;
}