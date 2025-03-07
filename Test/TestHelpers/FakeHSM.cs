using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Azure.Security.KeyVault.Keys;
using Org.BouncyCastle.Crypto.Engines;

namespace Test.TestHelpers;

public class FakeHsm
{
    private byte[] CreateTestKey()
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportPkcs8PrivateKey();
    }

    public byte[] SimulateHsm(KeyVaultKey kek)
    {
        // Generate the customer's key 
        var testKeyBytes = CreateTestKey();
        
        // Change the kek to a key that can be used 
        var cryptoClient = kek.Key.ToRSA();
        
        // Generate the AES key
        using var aesProvider = Aes.Create();
        aesProvider.KeySize = 256;
        aesProvider.GenerateKey();
        var aesKey = aesProvider.Key;

        // Encrypt the AES key using the KEK using RSA-OAEP with SHA1
        var encryptedAesKey = cryptoClient.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA1);
        
        // The plaintext (Customer's key) is encrypted using the AES key using AES Key Wrap with padding
        aesProvider.Mode = CipherMode.CFB;
        aesProvider.Padding = PaddingMode.PKCS7;
        aesProvider.GenerateIV();
        
        using var encryptor = aesProvider.CreateEncryptor();
        using var ms = new MemoryStream();
        
        ms.Write(aesProvider.IV, 0, aesProvider.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(testKeyBytes, 0, testKeyBytes.Length);
            cs.FlushFinalBlock();
        }
            
        byte[] encryptedKeyMaterial = AesKeyWrapWithPadding(testKeyBytes, aesKey);
        
        // The Aes key and key material (both encrypted) are concatenated to produce the ciphertext
        byte[] ciphertext = new byte[encryptedAesKey.Length + encryptedKeyMaterial.Length];
        Buffer.BlockCopy(encryptedAesKey, 0, ciphertext, 0, encryptedAesKey.Length);
        Buffer.BlockCopy(encryptedKeyMaterial, 0, ciphertext, encryptedAesKey.Length, encryptedKeyMaterial.Length);
        
        return ciphertext;
    }

    private byte[] AesKeyWrapWithPadding(byte[] keyToWrap, byte[] aesKey)
    {
        IWrapper wrapper = new AesWrapPadEngine();
        wrapper.Init(true, new KeyParameter(aesKey));
        return wrapper.Wrap(keyToWrap, 0, keyToWrap.Length);
    }
}