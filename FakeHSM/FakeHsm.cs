using System.Security.Cryptography;
using Azure.Security.KeyVault.Keys;
using FakeHSM.Interfaces;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace FakeHSM;

public enum Algorithm
{
    RSA,
    AES
}

public class KeyProperties(Algorithm algorithm, int keySize) 
{
    public Algorithm Algorithm { get; set; } = algorithm;
    public int KeySize { get; set; } = keySize;
}

public class KeyEntry(Object keyObject, Algorithm algorithm, int keySize)
{
    internal Object KeyObject { get; set; } = keyObject;
    public KeyProperties KeyProperties { get; set; } = new(algorithm, keySize);
}

public class FakeHsm : IFakeHsm
{
    private Dictionary<string,KeyEntry> _keys;
    
    public FakeHsm()
    {
        _keys = new Dictionary<string, KeyEntry>();
    }

    public (string randomId, byte[] pk) GenerateRsaKey(int bitLength)
    {
        var rsa = RSA.Create(bitLength);
        var pk = rsa.ExportSubjectPublicKeyInfo();
        var keyEntry = new KeyEntry(rsa, Algorithm.RSA, bitLength);
        var randomId = Guid.NewGuid().ToString();
        _keys.Add(randomId, keyEntry);
        return (randomId, pk);
    }
    public string GenerateAesKey(int bitLength)
    {
        // Generate the AES key
        var aes = Aes.Create();
        aes.KeySize = bitLength;
        aes.GenerateKey();
        var keyEntry = new KeyEntry(aes, Algorithm.AES, bitLength);
        var randomId = Guid.NewGuid().ToString();
        _keys.Add(randomId, keyEntry);
        return (randomId);
    }

    public byte[] GetPublicKeyOfId(string id)
    {
        if (!_keys.TryGetValue(id, out KeyEntry? keyEntry))
        {
            throw new KeyNotFoundException();
        }
        if (GetKeyProperties(id).Algorithm != Algorithm.RSA) throw new Exception("Key is not an RSA key");

        var rsa = (RSA)keyEntry.KeyObject;
        return rsa.ExportSubjectPublicKeyInfo();
    }
    
    // get the ids of the keys
    public List<string> GetKeyIds()
    {
        return _keys.Keys.ToList();
    }

    public KeyProperties GetKeyProperties(string id)
    {
        return _keys[id].KeyProperties;
    }

    public byte[] EncryptWithKey(string id, byte[] data)
    {
        // check if the key exists
        if (!_keys.TryGetValue(id, out KeyEntry? keyEntry))
        {
            throw new KeyNotFoundException();
        }
        
        var keyObject = keyEntry.KeyObject;
        // Encrypt the AES key using the KEK using RSA-OAEP with SHA1
        if (GetKeyProperties(id).Algorithm != Algorithm.RSA) throw new Exception("The key with that id is not an RSA key");
        var rsa = (RSA)keyObject;
        return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA1);
    }

    public byte[] GenerateBlob(KeyVaultKey kek)
    {
        // Generate the customer's private key
        var (id ,_) = GenerateRsaKey(2048);
        var pk = GetPrivateKey(id);
        
        // Convert the KEK to an RSA object
        var rsa = kek.Key.ToRSA();
        
        // Generate the AES key
        var aesKeyId = GenerateAesKey(256);
        var aesKey = GetAesKey(aesKeyId);
        
        // Encrypt the AES key using the KEK using RSA-OAEP with SHA1
        var encryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA1);
        
        
        var aes = Aes.Create();
        aes.Mode = CipherMode.CFB;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(pk, 0, pk.Length);
            cs.FlushFinalBlock();
        }
            
        var encryptedKeyMaterial = AesKeyWrapWithPadding(pk, aesKey);
        
        // The Aes key and key material (both encrypted) are concatenated to produce the ciphertext
        var ciphertext = new byte[encryptedAesKey.Length + encryptedKeyMaterial.Length];
        Buffer.BlockCopy(encryptedAesKey, 0, ciphertext, 0, encryptedAesKey.Length);
        Buffer.BlockCopy(encryptedKeyMaterial, 0, ciphertext, encryptedAesKey.Length, encryptedKeyMaterial.Length);
        
        return ciphertext;
    }

    private byte[] GetPrivateKey(string id)
    {
        // check if the key exists
        if (!_keys.TryGetValue(id, out KeyEntry? keyEntry))
        {
            throw new KeyNotFoundException();
        }
        // check if the key is an RSA key
        if (GetKeyProperties(id).Algorithm != Algorithm.RSA) throw new Exception("Key is not an RSA key");

        var rsa = (RSA)keyEntry.KeyObject;
        return rsa.ExportPkcs8PrivateKey();
    }

    private byte[] GetAesKey(string id)
    {
        // check if the key exists
        if (!_keys.TryGetValue(id, out var keyEntry))
        {
            throw new KeyNotFoundException();
        }
        // check if the key is an AES key
        if (GetKeyProperties(id).Algorithm != Algorithm.AES) throw new Exception("Key is not an AES key");

        var aes = (Aes)keyEntry.KeyObject;
        return aes.Key;
    }
    
    private byte[] AesKeyWrapWithPadding(byte[] keyToWrap, byte[] aesKey)
    {
        IWrapper wrapper = new AesWrapPadEngine();
        wrapper.Init(true, new KeyParameter(aesKey));
        return wrapper.Wrap(keyToWrap, 0, keyToWrap.Length);
    }
}