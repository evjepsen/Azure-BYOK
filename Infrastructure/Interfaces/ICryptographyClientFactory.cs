using Azure.Security.KeyVault.Keys.Cryptography;

namespace Infrastructure.Interfaces;

public interface ICryptographyClientFactory
{
    CryptographyClient GetCryptographyClient(Uri keyId);
}