using System.Security.Cryptography;
using FakeHSM.Interfaces;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace FakeHSM;


public class FakeHsm : IFakeHsm
{
    private ITokenService _tokenService;

    public FakeHsm(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    private byte[] GeneratePrivateRsaKey(int bitLength)
    {
        var rsa = RSA.Create(bitLength);
        var sk = rsa.ExportPkcs8PrivateKey();
        return sk;
    }
    private byte[] GenerateAesKey(int bitLength)
    {
        // Generate the AES key
        var aes = Aes.Create();
        aes.KeySize = bitLength;
        aes.GenerateKey();
        return aes.Key;
    }

    public string EncryptPrivateKeyForUpload(RSA rsaKek)
    {
        var ciphertext = EncryptCustomerHsmChosenKey(rsaKek);
        return Convert.ToBase64String(ciphertext);
    }

    private byte[] EncryptCustomerHsmChosenKey(RSA rsaKek)
    {
        // Generate the customer's private key
        var sk = GeneratePrivateRsaKey(2048);
        // Convert the KEK to an RSA object
        var rsa = rsaKek;
        
        // Generate the AES key
        var aesKey= GenerateAesKey(256);
        
        // GeneratePrivateKeyForBlob the AES key using the KEK using RSA-OAEP with SHA1
        var encryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA1);
        
        
        // The plaintext (Customer's key) is encrypted using the AES key using AES Key Wrap with padding
        var aes = Aes.Create();
        aes.Mode = CipherMode.CFB;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(sk, 0, sk.Length);
            cs.FlushFinalBlock();
        }
            
        var encryptedKeyMaterial = AesKeyWrapWithPadding(sk, aesKey);
        
        // The Aes key and key material (both encrypted) are concatenated to produce the ciphertext
        var ciphertext = new byte[encryptedAesKey.Length + encryptedKeyMaterial.Length];
        Buffer.BlockCopy(encryptedAesKey, 0, ciphertext, 0, encryptedAesKey.Length);
        Buffer.BlockCopy(encryptedKeyMaterial, 0, ciphertext, encryptedAesKey.Length, encryptedKeyMaterial.Length);
        
        return ciphertext;
    }

    public KeyTransferBlob GenerateBlobForUpload(RSA kek, string kekId)
    {
        var customerKey = EncryptCustomerHsmChosenKey(kek);
        var blob = _tokenService.CreateKeyTransferBlob(customerKey, kekId);
        return blob;
    }

    private byte[] AesKeyWrapWithPadding(byte[] keyToWrap, byte[] aesKey)
    {
        IWrapper wrapper = new AesWrapPadEngine();
        wrapper.Init(true, new KeyParameter(aesKey));
        return wrapper.Wrap(keyToWrap, 0, keyToWrap.Length);
    }
    
}
