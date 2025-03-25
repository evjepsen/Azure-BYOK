using Azure.Security.KeyVault.Keys;

namespace FakeHSM.Interfaces;

public interface IFakeHsm
{
    (string randomId, byte[] pk) GenerateRsaKey(int bitLength);
    string GenerateAesKey(int bitLength);
    byte[]? GetPublicKeyOfId(string id);
    List<string> GetKeys();
    KeyProperties GetKeyProperties(string id);
    byte[] EncryptWithKey(string id, byte[] data);
    byte[] GenerateBlob(KeyVaultKey kek);
}