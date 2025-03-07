using System.Text;
using Infrastructure.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace Infrastructure;

public class TokenService : ITokenService
{
    public string CreateKeyTransferBlob(byte[] cipherText, string kekId)
    {
        var jsonObject = new
        {
            schema_version = "1.0.0",
            header = new
            {
                kid = kekId,                    // The id of the KEK
                alg = "dir",
                enc = "CKM_RSA_AES_KEY_WRAP"
            },
            ciphertext = Base64UrlEncoder.Encode(cipherText),
            generator = "BYOK v1.0; Azure Key Vault"
        };

        return JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
    }

    public string CreateBodyForRequest(string transferBlob)
    {
        // Encode the transfer blob in bytes
        byte[] bytes = Encoding.UTF8.GetBytes(transferBlob);
        string transferBlobBase64Encoded = Convert.ToBase64String(bytes);
        
        // Create the json object
        // The key part of the object follows the JsonWebKey structure as specified by "https://learn.microsoft.com/en-us/azure/key-vault/keys/byok-specification" 
        var jsonObject = new
        {
            key = new
            {
               kty = "RSA-HSM",
               key_ops = new [] {
                   "decrypt",
                   "encrypt",
               },
               key_hsm = transferBlobBase64Encoded 
            },
            attributes = new
            {
                enabled = true
            }
        };

        return JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
    }
}