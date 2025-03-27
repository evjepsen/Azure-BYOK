using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Infrastructure.Interfaces;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Helpers;
using Infrastructure.Models;
using Microsoft.Extensions.Configuration;

namespace Infrastructure;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
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
        var serializedKeyTransferBlob = TokenHelper.SerializeJsonObject(transferBlob);
        var bytes = Encoding.UTF8.GetBytes(serializedKeyTransferBlob);
        var transferBlobBase64Encoded = Convert.ToBase64String(bytes);
        
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

    public string GenerateAccessToken(List<Claim> claims)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Missing JWT Secret"))
        );
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials
        );
        
        return jwtTokenHandler.WriteToken(token);
        
    }
}