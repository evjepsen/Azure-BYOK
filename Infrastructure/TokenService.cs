using System.Text;
using System.Text.Json;
using Infrastructure.Interfaces;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Helpers;
using Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public class TokenService : ITokenService
{
    private readonly ILogger<TokenService> _logger;

    public TokenService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TokenService>();
    }
    
    public KeyTransferBlob CreateKeyTransferBlob(byte[] encryptedKey, string kekId)
    {
        _logger.LogInformation("Creating key transfer blob");
        var keyTransferBlob = new KeyTransferBlob
        {
            Header = new HeaderObject
            {
                Kid = kekId, // The id of the KEK
                Alg = "dir",
                Enc = "CKM_RSA_AES_KEY_WRAP"
            },
            Ciphertext = Base64UrlEncoder.Encode(encryptedKey)
        };

        return keyTransferBlob;
    }

    public UploadKeyRequestBody CreateBodyForRequest(KeyTransferBlob transferBlob, string[] keyOperations)
    {
        // Encode the transfer blob in bytes
        _logger.LogInformation("Serializing key transfer blob into Json");
        var serializedKeyTransferBlob = TokenHelper.SerializeObject(transferBlob);
        var bytes = Encoding.UTF8.GetBytes(serializedKeyTransferBlob);
        var transferBlobBase64Encoded = Convert.ToBase64String(bytes);
        
        // Create the json object
        _logger.LogInformation("Creating request body for key upload");
        var keyRequestBody = new UploadKeyRequestBody
        {
            Key = new CustomJwk
            {
                Kty = "RSA-HSM",
                KeyOps = keyOperations,
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