using System.Text;
using Infrastructure.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using Infrastructure.Helpers;
using Infrastructure.Models;

namespace Infrastructure;

public class TokenService : ITokenService
{
    public KeyTransferBlob CreateKeyTransferBlob(byte[] cipherText, string kekId)
    {
        var keyTransferBlob = new KeyTransferBlob
        {
            SchemaVersion = "1.0.0",
            Header = new HeaderObject
            {
                Kid = kekId, // The id of the KEK
                Alg = "dir",
                Enc = "CKM_RSA_AES_KEY_WRAP"
            },
            Ciphertext = Base64UrlEncoder.Encode(cipherText),
            Generator = "BYOK v1.0; Azure Key Vault"
        };

        return keyTransferBlob;
    }

    public UploadKeyRequestBody CreateBodyForRequest(KeyTransferBlob transferBlob)
    {
        // Encode the transfer blob in bytes
        string serializedKeyTransferBlob = TokenHelper.SerializeJsonObject(transferBlob);
        byte[] bytes = Encoding.UTF8.GetBytes(serializedKeyTransferBlob);
        string transferBlobBase64Encoded = Convert.ToBase64String(bytes);
        
        // Create the json object
        // The key part of the object follows the JsonWebKey structure as specified by "https://learn.microsoft.com/en-us/azure/key-vault/keys/byok-specification" 
        UploadKeyRequestBody keyRequestBody = new UploadKeyRequestBody
        {
            Key = new CustomJwk
            {
                Kty = "RSA-HSM",
                KeyOps = ["decrypt", "encrypt"],
                KeyHsm = transferBlobBase64Encoded
            },
            Attributes = new CustomJwkAttributes
            {
                Enabled = true
            }
        };
        
        return keyRequestBody;
    }
}