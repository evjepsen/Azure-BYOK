using Azure;
using Azure.Security.KeyVault.Keys;

namespace Infrastructure.Interfaces;

public interface IKeyVaultService
{
    public Response<KeyVaultKey> ImportKey(string name, string value, KeyType keyType);
}